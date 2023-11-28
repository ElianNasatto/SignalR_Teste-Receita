using System.ComponentModel.DataAnnotations.Schema;

namespace AplicationSignalR.Models
{
    [Table("[ContribuinteReceita]")]
    public class ContribuinteReceitaFederalModel
    {
        public int id { get; set; }
        [Column("Cnpj_Basico")]
        public string Cnpj_Basico { get; set; }
        public string Razao_Social { get; set; }
        public int Natureza_Juridica { get; set; }
        public int Qualificacao_Responsavel { get; set; }
        public decimal Capital_Social { get; set; }
        public int Porte_Empresa { get; set; }
        public string Ente_Federativo_Responsavel { get; set; }
    }
}
