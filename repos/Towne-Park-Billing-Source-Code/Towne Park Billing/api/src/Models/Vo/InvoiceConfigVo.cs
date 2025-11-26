namespace api.Models.Vo
{
    public class InvoiceConfigVo
    {
        public InvoiceConfigKey? Key { get; set; }
        public string? Value { get; set; }
    }

    public enum InvoiceConfigKey
    {
        TowneParksAddress = 126840000,
        TowneParksPhone = 126840001,
        TowneParksLegalName = 126840002,
        TowneParksPOBox = 126840003,
        TowneParksAccountNumber = 126840004,
        TowneParksABA = 126840005,
        TowneParksEmail = 126840006,
        UPPGlobalLegalName = 126840007,
    }
}
