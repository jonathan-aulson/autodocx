using api.Models.Vo;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;

namespace api.Services.Impl;

public class InvoiceDocumentTemplate
{
    private readonly Color _tableBorder = new Color(81, 125, 192);
    private readonly Color _tableBlue = new Color(235, 240, 249);
    private readonly Color _tableGray = new Color(242, 242, 242);

    public Document CreateDocument(CustomerDetailVo customer)
    {
        // Create a new MigraDoc document
        var document = new Document
        {
            Info =
            {
                Title = "Towne Park Invoice",
                Subject = "Example Towne Park Invoice.",
                Author = "Towne Park"
            }
        };

        DefineStyles(document);

        CreatePage(document, customer);

        return document;
    }

    private void DefineStyles(Document document)
    {
        // Get the predefined style Normal.
        var normalStyle = document.Styles["Normal"];
        // Because all styles are derived from Normal, the next line changes the 
        // font of the whole document. Or, more exactly, it changes the font of
        // all styles and paragraphs that do not redefine the font.
        if (normalStyle != null) normalStyle.Font.Name = "Verdana";

        var headerStyle = document.Styles[StyleNames.Header];
        headerStyle?.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);

        var footerStyle = document.Styles[StyleNames.Footer];
        footerStyle?.ParagraphFormat.AddTabStop("8cm", TabAlignment.Center);

        // Create a new style called Table based on style Normal
        var tableStyle = document.Styles.AddStyle("Table", "Normal");
        tableStyle.Font.Name = "Verdana";
        tableStyle.Font.Name = "Times New Roman";
        tableStyle.Font.Size = 9;

        // Create a new style called Reference based on style Normal
        var referenceStyle = document.Styles.AddStyle("Reference", "Normal");
        referenceStyle.ParagraphFormat.SpaceBefore = "5mm";
        referenceStyle.ParagraphFormat.SpaceAfter = "5mm";
        referenceStyle.ParagraphFormat.TabStops.AddTabStop("16cm", TabAlignment.Right);
    }

    private void CreatePage(Document document, CustomerDetailVo customer)
    {
        // Each MigraDoc document needs at least one section.
        var section = document.AddSection();

        // Create footer
        var paragraph = section.Footers.Primary.AddParagraph();
        paragraph.AddText("Towne Park · 450 Plymouth Road · Suite 300 · PA 19462 · USA");
        paragraph.Format.Font.Size = 9;
        paragraph.Format.Alignment = ParagraphAlignment.Center;

        // Create the text frame for the address
        var addressFrame = section.AddTextFrame();
        addressFrame.Height = "3.0cm";
        addressFrame.Width = "7.0cm";
        addressFrame.Left = ShapePosition.Left;
        addressFrame.RelativeHorizontal = RelativeHorizontal.Margin;
        addressFrame.Top = "5.0cm";
        addressFrame.RelativeVertical = RelativeVertical.Page;

        // Put sender in address frame
        paragraph = addressFrame.AddParagraph("Towne Park · 450 Plymouth Road · Suite 300");
        paragraph.Format.Font.Name = "Times New Roman";
        paragraph.Format.Font.Size = 7;
        paragraph.Format.SpaceAfter = 3;
        
        // We use an empty paragraph to move the first table below the address field.
        paragraph = section.AddParagraph();
        paragraph.Format.LineSpacing = "6.5cm";
        paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
        
        // Create the intro table
        var introTable = section.AddTable();
        introTable.Style = "Table";
        introTable.Borders.Color = _tableBorder;
        introTable.Borders.Width = 0.25;
        introTable.Borders.Left.Width = 0.5;
        introTable.Borders.Right.Width = 0.5;
        introTable.Rows.LeftIndent = 0;

        introTable.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Center;
        introTable.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Center;
        introTable.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Center;
        introTable.AddColumn("4cm").Format.Alignment = ParagraphAlignment.Center;
        var introHeading = introTable.AddRow();
        introHeading.Format.Font.Bold = true;
        introHeading.Shading.Color = _tableBlue;
        introHeading.Cells[0].AddParagraph("Invoice Number");
        introHeading.Cells[1].AddParagraph("Invoice Date");
        introHeading.Cells[2].AddParagraph("Payment Terms");
        introHeading.Cells[3].AddParagraph("Amount Due");
        var introRow = introTable.AddRow();
        introRow.Format.Font.Bold = true;
        introRow.Shading.Color = _tableGray;
        introRow.Cells[0].AddParagraph("11362307");
        introRow.Cells[1].AddParagraph(DateTime.Now.ToString("MMMM dd, yyyy"));
        introRow.Cells[2].AddParagraph("Due in 30 Days");
        introRow.Cells[3].AddParagraph("53,866.92");

        // Add two invisible rows for spacing
        introTable.AddRow().Borders.Visible = false;
        introTable.AddRow().Borders.Visible = false;
        
        // Create the item table
        var table = section.AddTable();
        table.Style = "Table";
        table.Borders.Color = _tableBorder;
        table.Borders.Width = 0.25;
        table.Borders.Left.Width = 0.5;
        table.Borders.Right.Width = 0.5;
        table.Rows.LeftIndent = 0;

        // Before you can add a row, you must define the columns
        var column = table.AddColumn("12cm");
        column.Format.Alignment = ParagraphAlignment.Center;

        column = table.AddColumn("4cm");
        column.Format.Alignment = ParagraphAlignment.Right;

        // Create the header of the table
        var row = table.AddRow();
        row.HeadingFormat = true;
        row.Format.Alignment = ParagraphAlignment.Center;
        row.Format.Font.Bold = true;
        row.Shading.Color = _tableBlue;
        row.Cells[0].AddParagraph("Description");
        row.Cells[0].Format.Font.Bold = false;
        row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
        row.Cells[0].VerticalAlignment = VerticalAlignment.Bottom;
        row.Cells[1].AddParagraph("Amount");
        row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
        row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        
        FillContent(addressFrame, table, customer);
    }

    private void FillContent(TextFrame addressFrame, Table table, CustomerDetailVo customer)
    {
        // Fill address in address text frame
        var paragraph = addressFrame.AddParagraph();
        paragraph.AddText(customer.SiteName ?? string.Empty);
        paragraph.AddLineBreak();
        paragraph.AddText(customer.AccountManager + ", " + customer.SiteName);
        //paragraph.AddLineBreak();
        //paragraph.AddText(customer.Address ?? string.Empty);
        //paragraph.AddLineBreak();
        //paragraph.AddText(customer.PostalCode + " " + customer.City);

        var baseFeeRow = table.AddRow();
        baseFeeRow.Shading.Color = _tableGray;
        baseFeeRow.Borders.Distance = "3pt";
        baseFeeRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        var baseFeeParagraph = baseFeeRow.Cells[0].AddParagraph();
        baseFeeParagraph.AddText("Total Base for Fee Calculation");
        baseFeeParagraph.AddTab();
        baseFeeParagraph.AddText("137,042.69");
        baseFeeParagraph.AddTab();
        baseFeeParagraph.AddText("Towne Park Fees for Services");
        baseFeeRow.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        baseFeeRow.Cells[1].AddParagraph("67,150.92");

        var validatedParkingRow = table.AddRow();
        validatedParkingRow.Shading.Color = _tableGray;
        validatedParkingRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        validatedParkingRow.Cells[0].AddParagraph("Towne Park Fees for Validated Parking");
        validatedParkingRow.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        validatedParkingRow.Cells[1].AddParagraph("-");
        
        var midMonthBillingRow = table.AddRow();
        midMonthBillingRow.Shading.Color = _tableGray;
        midMonthBillingRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        midMonthBillingRow.Cells[0].AddParagraph("Less: Mid-Month Billing");
        midMonthBillingRow.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        midMonthBillingRow.Cells[1].AddParagraph("(12,000.00)");
        
        var expensesRow = table.AddRow();
        expensesRow.Shading.Color = _tableGray;
        expensesRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
        expensesRow.Cells[0].AddParagraph("Less: Expenses Paid by Hotel");
        expensesRow.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        expensesRow.Cells[1].AddParagraph("(1,284.00)");

        // Add an invisible row as a space line to the table
        var row = table.AddRow();
        row.Borders.Visible = false;

        // Add the total price row
        row = table.AddRow();
        row.Cells[0].Borders.Visible = false;
        row.Cells[0].AddParagraph("Total Due Towne Park");
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
        row.Cells[1].AddParagraph("53,866.92");
        row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
        row.Cells[1].Format.Font.Bold = true;
        row.Cells[1].Shading.Color = _tableGray;
    }
}