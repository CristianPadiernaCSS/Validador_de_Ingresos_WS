using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using ValidadorDeIngresosWS.Dtos;

namespace ValidadorDeIngresosWS.Services
{
    public class SoapValidatorRequestBuilder
    {
        public static string ParametrosValidadorXml(ParametersValidadorDto parametersValidadorDto)
        {
            parametersValidadorDto = parametersValidadorDto ?? throw new ArgumentNullException(nameof(parametersValidadorDto));
            // Datos de la consulta (ajustar según el servicio)
            return $@"<parametrosValidador xsi:type=""dto:ParametersValidadorDTO"" xmlns:dto=""http://dto.validador.ws.co"">
            <autorizacion xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.Autorizacion}</autorizacion>
            <idProducto xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.IdProducto}</idProducto>
            <numeroIdentificacion xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.NumeroIdentificacion}</numeroIdentificacion>
            <salarioReportado xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.SalarioReportado}</salarioReportado>
            <tipoIdentificacion xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.TipoIdentificacion}</tipoIdentificacion>
            <usuarioId xsi:type=""soapenc:string"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"">{parametersValidadorDto.UsuarioId}</usuarioId>
            </parametrosValidador>";
        }
    }
}
