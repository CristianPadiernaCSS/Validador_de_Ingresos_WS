using Newtonsoft.Json;
using System.Xml.Linq;

namespace ValidadorDeIngresosWS.Dtos
{
    public class ValidatorResponseDto
    {
        public TerceroDto Tercero { get; set; }
        public ConsumoDto Consumo { get; set; }

        public static ValidatorResponseDto FromSoapResponse(string soapXml)
        {
            var soapDoc = XDocument.Parse(soapXml);
            var returnNode = soapDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "consultaValidadorReturn");
            if (returnNode == null)
                throw new Exception("No se encontró consultaValidadorReturn");

            var rawXml = System.Net.WebUtility.HtmlDecode(returnNode.Value);
            var validadorXml = XDocument.Parse(rawXml);

            var tercero = validadorXml.Descendants("Tercero").FirstOrDefault();
            var consumo = validadorXml.Descendants("consumo").FirstOrDefault();

            return new ValidatorResponseDto
            {
                Tercero = tercero != null ? new TerceroDto
                {
                    IdentificadorLinea = tercero.Element("IdentificadorLinea")?.Value,
                    TipoIdentificacion = tercero.Element("TipoIdentificacion")?.Value,
                    CodigoTipoIndentificacion = tercero.Element("CodigoTipoIndentificacion")?.Value,
                    NumeroIdentificacion = tercero.Element("NumeroIdentificacion")?.Value,
                    NombreTitular = tercero.Element("NombreTitular")?.Value,
                    LugarExpedicion = tercero.Element("LugarExpedicion")?.Value,
                    FechaExpedicion = tercero.Element("FechaExpedicion")?.Value,
                    Estado = tercero.Element("Estado")?.Value,
                    NumeroInforme = tercero.Element("NumeroInforme")?.Value,
                    RangoEdad = tercero.Element("RangoEdad")?.Value,
                    CodigoDepartamento = tercero.Element("CodigoDepartamento")?.Value,
                    CodigoMunicipio = tercero.Element("CodigoMunicipio")?.Value,
                    Fecha = tercero.Element("Fecha")?.Value,
                    Hora = tercero.Element("Hora")?.Value,
                    Entidad = tercero.Element("Entidad")?.Value,
                    RespuestaConsulta = tercero.Element("RespuestaConsulta")?.Value,
                    Nombre1 = tercero.Element("Nombre1")?.Value,
                    Nombre2 = tercero.Element("Nombre2")?.Value,
                    Apellido1 = tercero.Element("Apellido1")?.Value,
                    Apellido2 = tercero.Element("Apellido2")?.Value,
                    CodigoEstado = tercero.Element("CodigoEstado")?.Value,
                } : null,

                Consumo = consumo != null ? new ConsumoDto
                {
                    Fecha = consumo.Element("fecha")?.Value,
                    ConsultaId = consumo.Element("consultaId")?.Value,
                    RespuestaId = consumo.Element("respuestaId")?.Value,
                    Variables = consumo.Element("Variables")?.Elements("Variable")
                        .Select(v => new VariableDto
                        {
                            Nombre = v.Element("Nombre")?.Value,
                            Valor = v.Element("Valor")?.Value
                        }).ToList(),
                    Aportantes = consumo.Elements("aportantes").Select(ap => new AportanteDto
                    {
                        TipoIdentificacionAportanteId = ap.Element("tipoIdentificacionAportanteId")?.Value,
                        NumeroIdentificacionAportante = ap.Element("numeroIdentificacionAportante")?.Value,
                        RazonSocialAportante = ap.Element("razonSocialAportante")?.Value,
                        TipoCotizantePersonaNatural = ap.Element("tipoCotizantePersonaNatural")?.Value,
                        Variables = ap.Element("Variables")?.Elements("Variable")?.Select(v => new VariableDto
                        {
                            Nombre = v.Element("Nombre")?.Value,
                            Valor = v.Element("Valor")?.Value
                        }).ToList(),
                        ResultadoPagos = ap.Elements("resultadoPagos").Select(rp => new ResultadoPagoDto
                        {
                            AnoPeriodoValidado = rp.Element("anoPeriodoValidado")?.Value,
                            MesPeriodoValidado = rp.Element("mesPeriodoValidado")?.Value,
                            RealizoPago = rp.Element("realizoPago")?.Value,
                            Ingresos = rp.Element("ingresos")?.Value,
                            Variables = rp.Element("Variables")?.Elements("Variable")?.Select(v => new VariableDto
                            {
                                Nombre = v.Element("Nombre")?.Value,
                                Valor = v.Element("Valor")?.Value
                            }).ToList(),
                        }).ToList(),
                        PromedioIngreso = ap.Element("promedioIngreso")?.Value,
                        MediasIngreso = ap.Element("mediasIngreso")?.Value,
                    }).ToList(),
                    SalidaException = new SalidaExceptionDto
                    {
                        CodigoError = consumo.Element("salidaException")?.Element("codigoError")?.Value,
                        MensajeError = consumo.Element("salidaException")?.Element("mensajeError")?.Value,
                    }
                } : null
            };
        }
    }
}
