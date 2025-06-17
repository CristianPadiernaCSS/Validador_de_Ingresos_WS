using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ValidadorDeIngresosWS.Services
{
    public class CustomSignedXml : SignedXml
    {
        private readonly X509Certificate2 _certificate;

        public CustomSignedXml(XmlDocument document, X509Certificate2 certificate) : base(document)
        {
            _certificate = certificate;

            // Verificar que el certificado tenga clave privada
            if (!_certificate.HasPrivateKey)
            {
                throw new Exception("El certificado no contiene una clave privada");
            }

            // Usar RSA para la firma
            var rsa = _certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new Exception("No se pudo obtener la clave privada RSA del certificado");
            }

            SigningKey = rsa;
        }
        public override XmlElement GetIdElement(XmlDocument document, string idValue)
        {
            if (string.IsNullOrEmpty(idValue))
                return null;

            // Remove # if present
            var cleanId = idValue.StartsWith("#") ? idValue.Substring(1) : idValue;

            Console.WriteLine($"Buscando elemento con ID: {cleanId}");

            var nsManager = CreateNamespaceManager(document);

            // Search by wsu:Id first (most common in WS-Security)
            var element = document.SelectSingleNode($"//*[@wsu:Id='{cleanId}']", nsManager);
            if (element != null)
            {
                Console.WriteLine($"Encontrado por wsu:Id: {element.LocalName}");
                return element as XmlElement;
            }

            // Search by generic Id
            element = document.SelectSingleNode($"//*[@Id='{cleanId}']", nsManager);
            if (element != null)
            {
                Console.WriteLine($"Encontrado por Id genérico: {element.LocalName}");
                return element as XmlElement;
            }

            // Search by any Id attribute (local-name approach)
            element = document.SelectSingleNode($"//*[@*[local-name()='Id']='{cleanId}']");
            if (element != null)
            {
                Console.WriteLine($"Encontrado por local-name Id: {element.LocalName}");
                return element as XmlElement;
            }

            Console.WriteLine($"ADVERTENCIA: No se encontró elemento con ID: {cleanId}");
            return null;
        }

        private XmlNamespaceManager CreateNamespaceManager(XmlDocument document)
        {
            var nsManager = new XmlNamespaceManager(document.NameTable);
            nsManager.AddNamespace("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
            nsManager.AddNamespace("wsse", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            nsManager.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            return nsManager;
        }
    }
}
