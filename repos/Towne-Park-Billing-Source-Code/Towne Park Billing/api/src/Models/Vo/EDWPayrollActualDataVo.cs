namespace api.Models.Vo
{
    public class EDWPayrollActualDataVo
    {
        public List<EDWPayrollDetailsRecord> Records { get; set; } = new();
    }

    public class EDWPayrollDetailsRecord
    {
        public string JobCode { get; set; }
        public decimal Hours { get; set; }
        public decimal Cost { get; set; }
        public System.DateTime Date { get; set; }
    }
}
