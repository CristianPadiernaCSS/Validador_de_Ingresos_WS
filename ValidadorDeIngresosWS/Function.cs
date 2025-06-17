using Amazon.Lambda.Core;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using ValidadorDeIngresosWS.Dtos;
using ValidadorDeIngresosWS.Services;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ValidadorDeIngresosWS;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<ValidatorResponseDto> FunctionHandler(ParametersValidadorDto input, ILambdaContext context)
    {
        //var datos = new ParametersValidadorDto
        //{
        //    Autorizacion = "S",
        //    IdProducto = "5718",
        //    NumeroIdentificacion = "1000001214",
        //    SalarioReportado = "1000000",
        //    TipoIdentificacion = "1",
        //    UsuarioId = "262499",
        //};

        var certificatePassword = Environment.GetEnvironmentVariable("certificatePassword");
        var username = Environment.GetEnvironmentVariable("username");
        var password = Environment.GetEnvironmentVariable("password");
        var serviceUrl = Environment.GetEnvironmentVariable("serviceUrl");
        string parametrosXml = SoapValidatorRequestBuilder.ParametrosValidadorXml(input);


        // Crear cliente y realizar consulta
        var client = new SecureSOAPClient(LoadEmbeddedCertificate(certificatePassword),
                                               username, password, serviceUrl);

        var response = await client.ConsultaValidadorAsync(parametrosXml);
        var dto = ValidatorResponseDto.FromSoapResponse(response);

        return dto;
    }

    private static X509Certificate2 LoadEmbeddedCertificate(string certificatePassword)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Reemplaza "ValidadorDeIngresosWS" con el namespace real de tu proyecto
        var resourceName = "ValidadorDeIngresosWS.Certificado.creditosPanda.pfx";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                // Si no encuentra el recurso, listar todos los disponibles para debug
                var availableResources = assembly.GetManifestResourceNames();
                var resourceList = string.Join(", ", availableResources);
                throw new FileNotFoundException($"Recurso embebido no encontrado: {resourceName}. Recursos disponibles: {resourceList}");
            }

            // Leer el archivo completo en memoria
            var certBytes = new byte[stream.Length];
            stream.Read(certBytes, 0, certBytes.Length);

            // Crear certificado desde bytes con password
            return new X509Certificate2(certBytes, certificatePassword,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }
    }
}
