using System.ComponentModel.DataAnnotations.Schema;

namespace AplicationSignalR.Models
{
    [Table("[ContribuinteReceita]")]
    public class ContribuinteReceitaFederalModel
    {
        public int id { get; set; }
        public string CnpjBasico { get; set; }
        public string RazaoSocial { get; set; }
        public int NaturezaJuridica { get; set; }
        public int QualificacaoResponsavel { get; set; }
        public decimal CapitalSocial { get; set; }
        public int PorteEmpresa { get; set; }
        public string EnteFederativoResponsavel { get; set; }
    }
}
