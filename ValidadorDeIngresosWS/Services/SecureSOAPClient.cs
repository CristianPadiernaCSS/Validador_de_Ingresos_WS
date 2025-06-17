using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ValidadorDeIngresosWS.Services
{
    public class SecureSOAPClient
    {
        private readonly HttpClient _httpClient;
        private readonly X509Certificate2 _certificate;
        private readonly string _serviceUrl;

        public SecureSOAPClient(X509Certificate2 certificate,
                               string username, string password, string serviceUrl)
        {
            try
            {
                _certificate = certificate;
                //new X509Certificate2(certificatePath, certificatePassword,
                //    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                // Validar certificado
                ValidateCertificate(_certificate);

                _serviceUrl = serviceUrl;

                // Configurar HttpClient con autenticación básica
                _httpClient = new HttpClient();
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

                // Configurar headers SOAP
                _httpClient.DefaultRequestHeaders.Add("SOAPAction", "");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inicializando cliente SOAP: {ex.Message}", ex);
            }
        }
        private void ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentException("El certificado no puede ser nulo");

            if (!certificate.HasPrivateKey)
                throw new ArgumentException("El certificado debe contener una clave privada");

            if (certificate.NotAfter < DateTime.Now)
                throw new ArgumentException($"El certificado ha expirado el {certificate.NotAfter}");

            if (certificate.NotBefore > DateTime.Now)
                throw new ArgumentException($"El certificado aún no es válido hasta {certificate.NotBefore}");

            // Verificar que tenga clave RSA
            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
                throw new ArgumentException("El certificado debe tener una clave privada RSA");
        }
        public async Task<string> ConsultaValidadorAsync(string parametrosValidadorXml)
        {
            try
            {
                // Construir el mensaje SOAP
                var soapMessage = BuildSOAPMessage(parametrosValidadorXml);

                // Firmar el mensaje
                var signedMessage = SignSOAPMessage(soapMessage);

                // Crear el contenido con el Content-Type correcto
                var content = new StringContent(signedMessage, Encoding.UTF8, "text/xml");

                // Agregar headers específicos al contenido si es necesario
                content.Headers.ContentType.CharSet = "utf-8";

                // Enviar la solicitud
                var response = await _httpClient.PostAsync(_serviceUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("=== RESPUESTA DE ERROR ===");
                    Console.WriteLine($"Status: {response.StatusCode}");
                    Console.WriteLine($"Reason: {response.ReasonPhrase}");
                    Console.WriteLine("Headers de respuesta:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    Console.WriteLine("Contenido:");
                    Console.WriteLine(FormatXml(responseContent));
                    Console.WriteLine("===========================");

                    throw new Exception($"Error: {responseContent}");
                }

                response.EnsureSuccessStatusCode();
                return responseContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                throw;
            }
        }

        private string FormatXml(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace
                });

                doc.Save(xmlWriter);
                return stringWriter.ToString();
            }
            catch
            {
                return xml; // Si no se puede formatear, retornar tal como está
            }
        }

        private string BuildSOAPMessage(string parametrosValidadorXml)
        {
            var timestamp = DateTime.UtcNow;
            var created = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            // Reduced to 5 minutes for better compatibility
            var expires = timestamp.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var timestampId = $"TS-{Guid.NewGuid().ToString("N")[..8]}";
            var bodyId = $"Body-{Guid.NewGuid().ToString("N")[..8]}";

            // Build SOAP with exact namespace structure expected by WSS4J
            var soapTemplate = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                  xmlns:ws=""http://ws.validador.ws.co""
                  xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <soapenv:Header>
        <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""
                       xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd""
                       soapenv:mustUnderstand=""1"">
            <wsu:Timestamp wsu:Id=""{timestampId}"">
                <wsu:Created>{created}</wsu:Created>
                <wsu:Expires>{expires}</wsu:Expires>
            </wsu:Timestamp>
        </wsse:Security>
    </soapenv:Header>
    <soapenv:Body xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" wsu:Id=""{bodyId}"">
        <ws:consultaValidador soapenv:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
            {parametrosValidadorXml}
        </ws:consultaValidador>
    </soapenv:Body>
</soapenv:Envelope>";

            return soapTemplate;
        }

        private string SignSOAPMessage(string soapMessage)
        {
            var xdoc = XDocument.Parse(soapMessage);
            // Convert back to XmlDocument if needed
            var doc = new XmlDocument();
            doc.LoadXml(xdoc.ToString());

            // Ensure namespace declarations are present
            EnsureNamespaceDeclarations(doc);

            // Crear el BinarySecurityToken
            var binarySecurityTokenId = $"X509-{Guid.NewGuid().ToString("N")[..16]}";
            var certificateBase64 = Convert.ToBase64String(_certificate.RawData);
            var securityElement = doc.SelectSingleNode("//*[local-name()='Security']") as XmlElement;

            if (securityElement == null)
            {
                throw new InvalidOperationException("Security element not found in SOAP message");
            }

            // Agregar BinarySecurityToken ANTES de la firma
            var binarySecurityToken = doc.CreateElement("wsse", "BinarySecurityToken",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");

            // Fix: Use proper namespace for wsu:Id attribute
            binarySecurityToken.SetAttribute("Id",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd",
                binarySecurityTokenId);
            binarySecurityToken.SetAttribute("ValueType",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");
            binarySecurityToken.SetAttribute("EncodingType",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
            binarySecurityToken.InnerText = certificateBase64;

            // Insertar después del Timestamp
            var timestampElement = securityElement.SelectSingleNode("*[local-name()='Timestamp']");
            if (timestampElement != null)
            {
                securityElement.InsertAfter(binarySecurityToken, timestampElement);
            }
            else
            {
                securityElement.AppendChild(binarySecurityToken);
            }

            // Crear la firma digital
            var signedXml = new CustomSignedXml(doc, _certificate);

            // Configurar algoritmos específicos para WS-Security
            signedXml.SignedInfo.CanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

            // Get actual IDs from the document
            var bodyId = GetBodyId(doc);
            var timestampId = GetTimestampId(doc);

            // Add Body and Timestamp references
            AddReferenceWithTransforms(signedXml, $"#{bodyId}");
            AddReferenceWithTransforms(signedXml, $"#{timestampId}");

            // Crear KeyInfo con SecurityTokenReference
            CreateKeyInfoWithSecurityTokenReference(signedXml, doc, binarySecurityTokenId);

            // Computar la firma
            signedXml.ComputeSignature();

            // Obtener el elemento de firma e insertarlo
            var signatureElement = signedXml.GetXml();

            // Import the signature element into the document's context
            var importedSignature = doc.ImportNode(signatureElement, true);
            securityElement.AppendChild(importedSignature);

            // Verificar estructura antes de retornar
            ValidateSignatureStructure(doc);

            return doc.OuterXml;
        }

        private void AddReferenceWithTransforms(SignedXml signedXml, string uri)
        {
            var reference = new Reference(uri);

            // Use exclusive canonicalization without comments
            var transform = new XmlDsigExcC14NTransform();
            reference.AddTransform(transform);

            // Use SHA-1 for digest (compatible with older WS-Security implementations)
            reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";

            signedXml.AddReference(reference);
        }

        private void CreateKeyInfoWithSecurityTokenReference(SignedXml signedXml, XmlDocument doc, string tokenId)
        {
            var keyInfo = new KeyInfo();

            // Create SecurityTokenReference element
            var securityTokenRef = doc.CreateElement("wsse", "SecurityTokenReference",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");

            // Create Reference element pointing to the BinarySecurityToken
            var reference = doc.CreateElement("wsse", "Reference",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            reference.SetAttribute("URI", $"#{tokenId}");
            reference.SetAttribute("ValueType",
                "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-x509-token-profile-1.0#X509v3");

            securityTokenRef.AppendChild(reference);

            // Add to KeyInfo
            keyInfo.AddClause(new KeyInfoNode(securityTokenRef));
            signedXml.KeyInfo = keyInfo;
        }
        private void ValidateSignatureStructure(XmlDocument doc)
        {
            // Validate that all required elements are present
            var security = doc.SelectSingleNode("//*[local-name()='Security']");
            var timestamp = doc.SelectSingleNode("//*[local-name()='Timestamp']");
            var binaryToken = doc.SelectSingleNode("//*[local-name()='BinarySecurityToken']");
            var signature = doc.SelectSingleNode("//*[local-name()='Signature']");

            if (security == null) throw new InvalidOperationException("Security element missing");
            if (timestamp == null) throw new InvalidOperationException("Timestamp element missing");
            if (binaryToken == null) throw new InvalidOperationException("BinarySecurityToken element missing");
            if (signature == null) throw new InvalidOperationException("Signature element missing");
        }
        private string GetTimestampId(XmlDocument doc)
        {
            var timestampElement = doc.SelectSingleNode("//*[local-name()='Timestamp']") as XmlElement;
            if (timestampElement == null)
                throw new InvalidOperationException("Timestamp element not found");

            var id = timestampElement.GetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            if (string.IsNullOrEmpty(id))
            {
                // Generate and set ID if not present
                id = $"TS-{Guid.NewGuid().ToString("N")[..8]}";
                timestampElement.SetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", id);
            }
            return id;
        }

        // FIXED: Added method to get Body ID
        private string GetBodyId(XmlDocument doc)
        {
            var bodyElement = doc.SelectSingleNode("//*[local-name()='Body']") as XmlElement;
            if (bodyElement == null)
                throw new InvalidOperationException("SOAP Body element not found");

            var id = bodyElement.GetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            if (string.IsNullOrEmpty(id))
            {
                // Generate and set ID if not present
                id = $"id-{Guid.NewGuid().ToString("N")[..8]}";
                bodyElement.SetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", id);
            }
            return id;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _certificate?.Dispose();
        }

        private void EnsureNamespaceDeclarations(XmlDocument doc)
        {
            var root = doc.DocumentElement;
            if (root == null) return;

            // Ensure common WS-Security namespaces are declared
            var namespaces = new Dictionary<string, string>
            {
                ["wsse"] = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd",
                ["wsu"] = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd",
                ["ds"] = "http://www.w3.org/2000/09/xmldsig#",
                ["xsi"] = "http://www.w3.org/2001/XMLSchema-instance"
            };

            foreach (var ns in namespaces)
            {
                var existingAttr = root.GetAttributeNode($"xmlns:{ns.Key}");
                if (existingAttr == null)
                {
                    root.SetAttribute($"xmlns:{ns.Key}", ns.Value);
                }
            }
        }
    }
}
