using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;
using Newtonsoft.Json;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class InvoiceDetailMapper
    {
        public static partial InvoiceDetailDto InvoiceDetailVoToDto(InvoiceDetailVo vo);

        private static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }

        [MapProperty(nameof(bs_Invoice.Fields.bs_Amount), nameof(InvoiceDetailVo.Amount))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_InvoiceDate), nameof(InvoiceDetailVo.InvoiceDate))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_InvoiceNumber), nameof(InvoiceDetailVo.InvoiceNumber))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_PaymentTerms), nameof(InvoiceDetailVo.PaymentTerms))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_Title), nameof(InvoiceDetailVo.Title))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_Description), nameof(InvoiceDetailVo.Description))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_PurchaseOrder), nameof(InvoiceDetailVo.PurchaseOrder))]
        [MapProperty(nameof(bs_Invoice.Fields.bs_BillingStatementFK), nameof(InvoiceDetailVo.BillingStatementFK))]
        private static partial InvoiceDetailVo MapInvoiceDetailModelToVo(bs_Invoice model);

        public static InvoiceDetailVo InvoiceDetailModelToVo(bs_Invoice model)
        {
            var vo = MapInvoiceDetailModelToVo(model);

            // Deserialize the invoice data
            if (!string.IsNullOrEmpty(model.bs_InvoiceData))
            {
                vo.LineItems = JsonConvert.DeserializeObject<List<LineItemVo>>(model.bs_InvoiceData);
            }

            if (model.bs_InvoiceGroupFK != null)
            {
                vo.InvoiceGroupFK = model.bs_InvoiceGroupFK.Id;
            }

            return vo;
        }

        public static partial IEnumerable<LineItemVo> LineItemsDtoToVo(IEnumerable<LineItemDto> dto);

        private static partial IEnumerable<LineItemDto> LineItemsVoToDto(IEnumerable<LineItemVo> vo);

        [MapProperty(nameof(LineItemVo.MetaData), nameof(LineItemDto.MetaData))]
        public static partial LineItemDto MapLineItemVoToDto(LineItemVo vo);

        public static string LineItemsVoToModel(IEnumerable<LineItemVo> vo)
        {
            return JsonConvert.SerializeObject(LineItemsVoToDto(vo));
        }

        public static partial MetaDataDto MapMetaDataToDto(MetaDataVo vo);
    }
}
