using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
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
        return await HandlePostAsync(request);
        //return request.RequestContext.HttpMethod?.ToUpper() switch
        //{
        //    "POST" => await HandlePostAsync(request),
        //    _ => CreateResponse(HttpStatusCode.MethodNotAllowed, new
        //    {
        //        error = "Método no permitido."
        //    })
        //};
    }

    private APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" } // Permite CORS público
            }
        };
    }

    private async Task<APIGatewayProxyResponse> HandlePostAsync(APIGatewayProxyRequest request)
    {
        try
        {
            // Parsear el body JSON a tu DTO
            var input = JsonSerializer.Deserialize<ParametersValidadorDto>(request.Body);
            var response = await ProcesarConsultaAsync(input);

            var json = JsonSerializer.Serialize(response);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = json,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" } // para CORS
                }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { error = ex.Message })
            };
        }
    }
    private async Task<ValidatorResponseDto> ProcesarConsultaAsync(ParametersValidadorDto input)
    {
        var certificatePassword = Environment.GetEnvironmentVariable("certificatePassword");
        var username = Environment.GetEnvironmentVariable("username");
        var password = Environment.GetEnvironmentVariable("password");
        var serviceUrl = Environment.GetEnvironmentVariable("serviceUrl");
        string parametrosXml = SoapValidatorRequestBuilder.ParametrosValidadorXml(input);

        if (string.IsNullOrEmpty(certificatePassword))
            throw new Exception("La contraseña del certificado es null o vacía");


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
