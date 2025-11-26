import { Button } from "@/components/ui/button";
import { ForecastData, Statement } from "@/lib/models/Statement";
import { formatCurrency } from "@/lib/utils";
import { useCustomerDetails } from "@/pages/customersDetails/CustomersDetailContext";
import { formatDate } from "date-fns";
import { CircleX, ScanEye, ShieldCheck } from "lucide-react";
import React, { useEffect, useState } from 'react';
import Modal from "../Modal";
import { Dialog, DialogContent, DialogDescription, DialogTitle } from "../ui/dialog";


interface Props {
    reportStatement: Statement;
    forecastData: ForecastData;
}

const convertServicePeriodToDate = (servicePeriod: string): string | null => {
    const parts = servicePeriod.split("-");
    const date = new Date(Date.parse(`${parts[1].trim()}`));

    if (isNaN(date.getTime())) return null;
    return formatDate(date, 'yyyyMM');
}

const SupportingDocuments: React.FC<Props> = ({ reportStatement, forecastData }) => {
    const [showModal, setShowModal] = useState(false);
    const { contractDetails } = useCustomerDetails();
    const [open, setOpen] = useState(false);
    const [pdfData, setPdfData] = useState<string | null>(null);
    const [selectedReportTitle, setSelectedReportTitle] = useState<string | null>(null);
    const [availableReports, setAvailableReports] = useState<any[]>([]);

    const servicePeriodDate = convertServicePeriodToDate(reportStatement.servicePeriod);
    const openModal = () => setShowModal(true);
    const closeModal = () => setShowModal(false);

    const openReport = async (reportTitle: string) => {
        try {
            const response = await fetch(`/api/reports/${servicePeriodDate}/${reportStatement.siteNumber}/${reportTitle}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/pdf',
                    'Referrer-Policy': 'strict-origin-when-cross-origin'
                }
            });
            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            setPdfData(url);
            setSelectedReportTitle(reportTitle);
            setOpen(true);
        } catch (error) {
            console.error('Error fetching PDF:', error);
        }
    };

    const formatReportTitle = (text: string): string => {
        return text
            .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
            .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
            .replace(/[_-]/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    };

    const fetchAvailableReports = async () => {
        try {
            const response = await fetch(`/api/reports/${servicePeriodDate}/${reportStatement.siteNumber}`);
            const data = await response.json();
            const reports = data.map((report: any) => ({
                title: formatReportTitle(report),
                icon: <ScanEye className="h-4 w-4" />,
                name: report
            }));
            setAvailableReports(reports);
        } catch (error) {
            console.error('Error fetching available reports:', error);
        }
    };

    useEffect(() => {
        fetchAvailableReports();
    }, []);

    return (
        <>
            <div className="my-1 mx-2">
                <div className="my-1 mx-2">
                    <p className="mb-2 text-gray-500 mt-1">Supporting Documents</p>
                    <ul className="border rounded-lg">
                        <li className="grid grid-cols-[1fr_auto] items-center px-3 py-2 border-b transition-colors hover:bg-muted/50">
                            <span className="font-bold">Towne Park - Forecast Comparison Report</span>
                            <Button data-qa-id="button-viewForecastReport-reports" variant="ghost" onClick={openModal}>
                                <ScanEye className="h-4 w-4" />
                            </Button>
                        </li>
                        {(availableReports).map((report, index) => (
                            <li key={index} className="grid grid-cols-[1fr_auto] items-center px-3 py-2 border-b last:border-b-0 transition-colors hover:bg-muted/50">
                                <span>{report.title}</span>
                                <Button data-qa-id={`button-viewReport-${report.name}-${index}-reports`} variant="ghost" onClick={() =>openReport(report.name)}>
                                    {report.icon}
                                </Button>
                            </li>
                        ))}
                    </ul>
                </div>

                <Dialog data-qa-id="dialog-reportViewer-reports" open={open} onOpenChange={setOpen}>
                    <DialogContent className="w-[90%] max-w-[95vw] h-[90%] max-h-[95vh] flex flex-col">
                        <DialogTitle>
                        {selectedReportTitle && formatReportTitle(selectedReportTitle)}
                        </DialogTitle>
                        <DialogDescription className="flex-1 w-full">
                            {pdfData && (
                                <iframe
                                    data-qa-id="iframe-reportContent-reports"
                                    src={pdfData}
                                    className="w-full h-full border rounded overflow-auto resize-none"
                                />
                            )}
                        </DialogDescription>
                    </DialogContent>
                </Dialog>



                <Modal data-qa-id="modal-forecastComparison-reports" show={showModal} onClose={closeModal}>
                    <div className="p-6 rounded-lg w-full max-w-3xl mx-auto">
                        <h2 className="font-bold text-xl mb-4">
        
                        Comparison To Forecasted Revenue (Site: {reportStatement.siteNumber}, Period: {new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(new Date(reportStatement.createdMonth))})
                        </h2>
                        <div>
                            <div className="flex justify-between border-b py-1 mb-6 font-bold" data-testid="forecasted-revenue">
                                <span>Forecasted Revenue</span>
                                <span>{formatCurrency(forecastData?.forecastedRevenue)}</span>
                            </div>

                            <div className="flex justify-between py-1" data-testid="posted-revenue">
                                <span>Posted Revenue/Account Summary</span>
                                <span>{formatCurrency(forecastData?.postedRevenue)}</span>
                            </div>
                            <div className="flex justify-between py-1" data-testid="invoiced-revenue">
                                <span>Invoiced Revenue</span>
                                <span>+ {formatCurrency(forecastData?.invoicedRevenue)}</span>
                            </div>
                            <div className="flex justify-between border-t mt-2 pt-2 py-1" data-testid="total-actual-revenue">
                                <span className="font-bold">Total Actual Revenue</span>
                                <span>{formatCurrency(forecastData?.totalActualRevenue)}</span>
                            </div>
                            <div className="flex justify-between py-1 mt-4" data-testid="deviation-percentage">
                                <div className="flex items-center">
                                    {contractDetails?.deviationPercentage !== undefined &&
                                        contractDetails.deviationPercentage < forecastData?.forecastDeviationPercentage ? (
                                        <CircleX className="text-red-500 mr-1" />
                                    ) : (
                                        <ShieldCheck className="text-green-500 mr-1" />
                                    )}
                                    <span>Forecast Deviation Percentage</span>
                                </div>
                                <span>
                                    {forecastData?.forecastDeviationPercentage?.toFixed(2)}%
                                    (Threshold: {contractDetails?.deviationPercentage?.toFixed(2)}%)
                                </span>
                            </div>
                            <div className="flex justify-between py-1" data-testid="deviation-amount">
                                <div className="flex items-center">
                                    {contractDetails?.deviationAmount !== undefined &&
                                        contractDetails.deviationAmount < forecastData?.forecastDeviationAmount ? (
                                        <CircleX className="text-red-500 mr-1" />
                                    ) : (
                                        <ShieldCheck className="text-green-500 mr-1" />
                                    )}
                                    <span>Forecast Deviation Amount</span>
                                </div>
                                <span>
                                    {formatCurrency(forecastData?.forecastDeviationAmount)}
                                    (Threshold: {formatCurrency(contractDetails?.deviationAmount || 0)})
                                </span>
                            </div>
                        </div>
                    </div>
                    <div className="text-sm text-gray-500 mt-4">
                        <span>
                            Forecast Last Updated: {forecastData?.forecastLastUpdated ? new Date(forecastData?.forecastLastUpdated)?.toLocaleString() : ""}
                        </span>
                    </div>
                </Modal>
            </div>
        </>
    );
};


export default SupportingDocuments;
