namespace ValidadorDeIngresosWS.Dtos
{
    public class TerceroDto
    {
        public string IdentificadorLinea { get; set; }
        public string TipoIdentificacion { get; set; }
        public string CodigoTipoIndentificacion { get; set; }
        public string NumeroIdentificacion { get; set; }
        public string NombreTitular { get; set; }
        public string LugarExpedicion { get; set; }
        public string FechaExpedicion { get; set; }
        public string Estado { get; set; }
        public string NumeroInforme { get; set; }
        public string RangoEdad { get; set; }
        public string CodigoDepartamento { get; set; }
        public string CodigoMunicipio { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public string Entidad { get; set; }
        public string RespuestaConsulta { get; set; }
        public string Nombre1 { get; set; }
        public string Nombre2 { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }
        public string CodigoEstado { get; set; }
    }

    public class ConsumoDto
    {
        public string Fecha { get; set; }
        public string ConsultaId { get; set; }
        public string RespuestaId { get; set; }
        public List<VariableDto> Variables { get; set; }
        public List<AportanteDto> Aportantes { get; set; }
        public SalidaExceptionDto SalidaException { get; set; }
    }

    public class AportanteDto
    {
        public string TipoIdentificacionAportanteId { get; set; }
        public string NumeroIdentificacionAportante { get; set; }
        public string RazonSocialAportante { get; set; }
        public string TipoCotizantePersonaNatural { get; set; }
        public List<VariableDto> Variables { get; set; }
        public List<ResultadoPagoDto> ResultadoPagos { get; set; }
        public string PromedioIngreso { get; set; }
        public string MediasIngreso { get; set; }
    }

    public class ResultadoPagoDto
    {
        public string AnoPeriodoValidado { get; set; }
        public string MesPeriodoValidado { get; set; }
        public string RealizoPago { get; set; }
        public string Ingresos { get; set; }
        public List<VariableDto> Variables { get; set; }
    }

    public class VariableDto
    {
        public string Nombre { get; set; }
        public string Valor { get; set; }
    }

    public class SalidaExceptionDto
    {
        public string CodigoError { get; set; }
        public string MensajeError { get; set; }
    }

}
