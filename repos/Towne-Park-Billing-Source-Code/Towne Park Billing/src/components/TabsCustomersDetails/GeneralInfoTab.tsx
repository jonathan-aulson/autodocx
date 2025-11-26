import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { useAuth } from "@/contexts/AuthContext";
import { CustomerDetail, GeneralInfoSchema } from "@/lib/models/GeneralInfo";
import { cn } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { format, parseISO } from "date-fns";
import { CalendarIcon } from "lucide-react";
import React, { useState } from 'react';
import { SubmitHandler, useForm } from "react-hook-form";
import { PulseLoader } from "react-spinners";
import { Button } from '../ui/button';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "../ui/form";
import { Input } from '../ui/input';
import { Separator } from "../ui/separator";
import { Textarea } from "../ui/textarea";

interface GeneralInfoTabProps {
    customer: CustomerDetail | null;
    onSave: (customer: CustomerDetail) => void;
}

interface GeneralInfoFormValues {
    invoiceRecipient: string;
    glString: string;
    district: string;
    accountManager: string;
    accountManagerId: string;
    billingContactEmail: string;
    siteName: string;
    address: string;
    startDate: Date | null;
    closeDate: Date | null;
    siteNumber: string;
    totalRoomsAvailable: string;
    totalAvailableParking: string;
    districtManager: string;
    assistantDistrictManager: string;
    assistantAccountManager: string;
    vendorId: string;
    legalEntity: string;
    plCategory: string;
    cogSegment: string;
    svpRegion: string;
    businessSegment: string;
}

const GeneralInfoTab: React.FC<GeneralInfoTabProps> = ({ customer, onSave }) => {
    const [isEditing, setIsEditing] = useState(false);
    const [originalValues, setOriginalValues] = useState<GeneralInfoFormValues | null>(customer ? {
        ...customer,
        startDate: customer.startDate ? parseISO(customer.startDate) : null,
        closeDate: customer.closeDate ? parseISO(customer.closeDate) : null,
    } : null);
    const [isLoading, setIsLoading] = useState(false);
    const { userRoles } = useAuth();

    const form = useForm<GeneralInfoFormValues>({
        resolver: zodResolver(GeneralInfoSchema),
        defaultValues: customer ? {
            invoiceRecipient: customer.invoiceRecipient || "",
            accountManager: customer.accountManager || "",
            billingContactEmail: customer.billingContactEmail || "",
            siteName: customer.siteName || "",
            address: customer.address || "",
            glString: customer.glString || "",
            district: customer.district || "",
            accountManagerId: customer.accountManagerId || "",
            startDate: customer.startDate ? parseISO(customer.startDate) : null,
            closeDate: customer.closeDate ? parseISO(customer.closeDate) : null,
            totalRoomsAvailable: customer.totalRoomsAvailable,
            totalAvailableParking: customer.totalAvailableParking,
            districtManager: customer.districtManager || "",
            assistantDistrictManager: customer.assistantDistrictManager || "",
            assistantAccountManager: customer.assistantAccountManager || "",
            vendorId: customer.vendorId || "",
            legalEntity: customer.legalEntity || "",
            plCategory: customer.plCategory || "",
            cogSegment: customer.cogSegment || "",
            svpRegion: customer.svpRegion || "",
            businessSegment: customer.businessSegment || "",
        } : {},
    });
    

    const { register, handleSubmit, formState: { errors }, reset, watch, setValue } = form;

    const handleEdit = () => {
        setOriginalValues({
            ...customer,
            address: watch("address"),
            glString: watch("glString"),
            district: watch("district"),
            invoiceRecipient: watch("invoiceRecipient"),
            accountManager: watch("accountManager"),
            accountManagerId: watch("accountManagerId"),
            billingContactEmail: watch("billingContactEmail"),
            siteName: watch("siteName"),
            siteNumber: customer?.siteNumber || "",
            startDate: watch("startDate"),
            closeDate: watch("closeDate"),
            totalRoomsAvailable: watch("totalRoomsAvailable"),
            totalAvailableParking: watch("totalAvailableParking"),
            districtManager: watch("districtManager"),
            assistantDistrictManager: watch("assistantDistrictManager"),
            assistantAccountManager: watch("assistantAccountManager"),
            vendorId: watch("vendorId"),
            legalEntity: watch("legalEntity"),
            plCategory: watch("plCategory"),
            cogSegment: watch("cogSegment"),
            svpRegion: watch("svpRegion"),
            businessSegment: watch("businessSegment")
        });
        setIsEditing(true);
    };

    const handleCancel = () => {
        reset(originalValues!);
        setIsEditing(false);
    };

    const isBillingAdmin = userRoles.includes('billingAdmin');

    const onSubmit: SubmitHandler<GeneralInfoFormValues> = data => {
        const formattedData = {
            ...customer,
            address: data.address,
            glString: data.glString,
            district: data.district,
            invoiceRecipient: data.invoiceRecipient,
            accountManager: data.accountManager,
            accountManagerId: data.accountManagerId,
            billingContactEmail: data.billingContactEmail,
            siteName: data.siteName,
            siteNumber: customer?.siteNumber || "",
            startDate: data.startDate ? format(data.startDate, 'yyyy-MM-dd') : null,
            closeDate: data.closeDate ? format(data.closeDate, 'yyyy-MM-dd') : null,
            totalRoomsAvailable: data.totalRoomsAvailable,
            totalAvailableParking: data.totalAvailableParking,
            districtManager: data.districtManager,
            assistantDistrictManager: data.assistantDistrictManager,
            assistantAccountManager: data.assistantAccountManager,
            vendorId: data.vendorId,
            legalEntity: data.legalEntity,
            plCategory: data.plCategory,
            cogSegment: data.cogSegment,
            svpRegion: data.svpRegion,
            businessSegment: data.businessSegment,
        };
        onSave(formattedData);
        setIsEditing(false);
    };

    const handleFormSubmit = async (data: GeneralInfoFormValues) => {
        setIsLoading(true);
        await onSubmit(data);
        setIsLoading(false);
    };

    return (
        <div className="py-8">
        <Form {...form}>
            <form onSubmit={handleSubmit(handleFormSubmit)}>
                <div className="flex items-center justify-between mb-6">
                    <h2 className="text-2xl font-semibold">General Info</h2>
                    <div className="flex items-center gap-2">
                        {/* <Button variant="outline" size="sm">
                    <MoreVerticalIcon className="w-4 h-4 mr-2" />
                    More
                </Button> */}
                        {isEditing ? (
                            <div className="flex space-x-2">
                                <Button variant="outline" onClick={handleCancel} disabled={isLoading} data-qa-id="button-cancelGeneralInfo">Cancel</Button>
                                <Button type="submit" disabled={isLoading} data-qa-id="button-saveGeneralInfo">
                                    {isLoading ? <PulseLoader size={10} /> : "Save"}
                                </Button>
                            </div>
                        ) : (isBillingAdmin &&
                            <Button onClick={handleEdit} data-qa-id="button-editGeneralInfo">Edit</Button>
                        )}
                    </div>
                </div>
                <div className="grid gap-4 p-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="location-id">Site ID</FormLabel>
                            <Input id="location-id" value={customer?.siteNumber} disabled={true} data-qa-id="input-field-siteNumber" />
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="vendor-id">Vendor ID</FormLabel>
                            <Input
                                id="vendor-id"
                                {...register("vendorId")}
                                disabled={!isEditing}
                                data-qa-id="input-field-vendorId"
                            />
                          
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="site-name-id">GL String</FormLabel>
                            <Input
                                id="gl-code-id"
                                disabled={true}
                                value={watch("glString") || ""}
                                data-qa-id="input-field-glString"
                            />
                            {errors.glString && (
                                <p className="text-red-600">{errors.glString.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="site-name-id">Site Name</FormLabel>
                            <Input
                                id="site-name-id"
                                disabled={true}
                                value={watch("siteName") || ""}
                                data-qa-id="input-field-siteName"
                            />
                            {errors.siteName && (
                                <p className="text-red-600">{errors.siteName.message}</p>
                            )}
                        </div>
                        <div className="space-y-2 col-span-2">
                            <FormLabel htmlFor="address-id">Address</FormLabel>
                            <Input
                                id="address-id"
                                {...register("address")}
                                disabled={!isEditing}
                                data-qa-id="input-field-address"
                            />
                            {errors.address && (
                                <p className="text-red-600">{errors.address.message}</p>
                            )}
                        </div>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="total-rooms">Total Rooms Available</FormLabel>
                            <Input
                                id="total-rooms"
                                disabled={true}
                                value={watch("totalRoomsAvailable") || ""}
                                data-qa-id="input-field-totalRoomsAvailable"
                            />
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="total-parking">Total Available Parking</FormLabel>
                            <Input
                                id="total-parking"
                                disabled={true}
                                value={watch("totalAvailableParking") || ""}
                                data-qa-id="input-field-totalAvailableParking"
                            />
                        </div>
                    </div>

                    <FormLabel className="font-extrabold">Business Classification</FormLabel>
                    <Separator className="my-2" />

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="legal-entity">Legal Entity</FormLabel>
                            <Input
                                id="legal-entity"
                                {...register("legalEntity")}
                                disabled={!isEditing}
                                data-qa-id="input-field-legalEntity"
                            />
                            {errors.legalEntity && (
                                <p className="text-red-600">{errors.legalEntity.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="pl-category">PL Category</FormLabel>
                            <Input
                                id="pl-category"
                                {...register("plCategory")}
                                disabled={!isEditing}
                                data-qa-id="input-field-plCategory"
                            />
                            {errors.plCategory && (
                                <p className="text-red-600">{errors.plCategory.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="cog-segment">COG Segment</FormLabel>
                            <Input
                                id="cog-segment"
                                {...register("cogSegment")}
                                disabled={!isEditing}
                                data-qa-id="input-field-cogSegment"
                            />
                            {errors.cogSegment && (
                                <p className="text-red-600">{errors.cogSegment.message}</p>
                            )}
                        </div>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="svp-region">SVP Region</FormLabel>
                            <Input
                                id="svp-region"
                                value={watch("svpRegion") || ""}
                                disabled={true}
                                data-qa-id="input-field-svpRegion"
                            />
                            {errors.svpRegion && (
                                <p className="text-red-600">{errors.svpRegion.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="business-segment">Business Segment</FormLabel>
                            <Input
                                id="business-segment"
                                {...register("businessSegment")}
                                disabled={!isEditing}
                                data-qa-id="input-field-businessSegment"
                            />
                            {errors.businessSegment && (
                                <p className="text-red-600">{errors.businessSegment.message}</p>
                            )}
                        </div>
                    </div>


                    <FormLabel className="font-extrabold">Account Mgmt</FormLabel>
                    <Separator className="my-2" />

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="district-id">District</FormLabel>
                            <Input
                                id="district-id"
                                value={watch("district") || ""}
                                disabled={true}
                                data-qa-id="input-field-district"
                            />
                            {errors.district && (
                                <p className="text-red-600">{errors.district.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="district-manager">District Manager</FormLabel>
                            <Input
                                id="district-manager"
                                value={watch("districtManager") || ""}
                                disabled={true}
                                data-qa-id="input-field-districtManager"
                            />
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="assistant-district-manager">Assistant District Manager</FormLabel>
                            <Input
                                id="assistant-district-manager"
                                {...register("assistantDistrictManager")}
                                disabled={!isEditing}
                                data-qa-id="input-field-assistantDistrictManager"
                            />
                        </div>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="account-manager">Account Manager</FormLabel>
                            <Input
                                id="account-manager"
                                {...register("accountManager")}
                                disabled={!isEditing}
                                data-qa-id="input-field-accountManager"
                            />
                            {errors.accountManager && (
                                <p className="text-red-600">{errors.accountManager.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="account-manager-id">Account Manager ID</FormLabel>
                            <Input
                                id="account-manager-id"
                                {...register("accountManagerId")}
                                disabled={!isEditing}
                                data-qa-id="input-field-accountManagerId"
                            />
                            {errors.accountManagerId && (
                                <p className="text-red-600">{errors.accountManagerId.message}</p>
                            )}
                        </div>
                        <div className="space-y-2">
                            <FormLabel htmlFor="assistant-account-manager">Assistant Account Manager</FormLabel>
                            <Input
                                id="assistant-account-manager"
                                {...register("assistantAccountManager")}
                                disabled={!isEditing}
                                data-qa-id="input-field-assistantAccountManager"
                            />
                        </div>
                    </div>

                    <FormLabel className="font-extrabold">
                        Billing Info
                    </FormLabel>
                    <Separator className="my-2" />

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                        <div className="space-y-2">
                            <FormLabel htmlFor="billing-contact-email-id">Billing Contact Emails</FormLabel>
                            <Textarea
                                id="billing-contact-email-id"
                                {...register("billingContactEmail")}
                                className="w-full border rounded-md p-2"
                                rows={3}
                                disabled={!isEditing}
                                data-qa-id="textarea-field-billingContactEmail"
                            />
                            {errors.billingContactEmail && (
                                <p className="text-red-600">{errors.billingContactEmail.message}</p>
                            )}
                        </div>

                        <div className="space-y-2">
                            <FormLabel htmlFor="invoice-recipient">Invoice Recipient</FormLabel>
                            <Input
                                id="invoice-recipient-id"
                                {...register("invoiceRecipient")}
                                disabled={!isEditing}
                                data-qa-id="input-field-invoiceRecipient"
                            />
                            {errors.invoiceRecipient && (
                                <p className="text-red-600">{errors.invoiceRecipient.message}</p>
                            )}
                        </div>

                        {/* Start Date */}
                        <FormField
                            control={form.control}
                            name="startDate"
                            render={({ field }) => (
                                <FormItem className="flex flex-col">
                                    <FormLabel>Start Date</FormLabel>
                                    <Popover>
                                        <PopoverTrigger asChild>
                                            <FormControl>
                                                <Button
                                                    disabled={true}
                                                    variant={"outline"}
                                                    className={cn(
                                                        "w-[240px] pl-3 text-left font-normal",
                                                        !field.value && "text-muted-foreground"
                                                    )}
                                                    data-qa-id="button-startDatePicker"
                                                >
                                                    {field.value ? (
                                                        format(field.value, "PPP")
                                                    ) : (
                                                        <span>Pick a date</span>
                                                    )}
                                                    <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                                                </Button>
                                            </FormControl>
                                        </PopoverTrigger>
                                        <PopoverContent className="w-auto p-0" align="start">
                                            <Calendar
                                                mode="single"
                                                selected={field.value || undefined}
                                                onSelect={(date) => setValue("startDate", date ?? null)}
                                                disabled={(date) =>
                                                    date < new Date("1900-01-01")
                                                }
                                                initialFocus
                                                data-qa-id="calendar-startDate"
                                            />
                                        </PopoverContent>
                                    </Popover>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Close Date */}
                        <FormField
                            control={form.control}
                            name="closeDate"
                            render={({ field }) => (
                                <FormItem className="flex flex-col">
                                    <FormLabel>Close Date</FormLabel>
                                    <Popover>
                                        <PopoverTrigger asChild>
                                            <FormControl>
                                                <Button
                                                    disabled={true}
                                                    variant={"outline"}
                                                    className={cn(
                                                        "w-[240px] pl-3 text-left font-normal",
                                                        !field.value && "text-muted-foreground"
                                                    )}
                                                    data-qa-id="button-closeDatePicker"
                                                >
                                                    {field.value ? (
                                                        format(field.value, "PPP")
                                                    ) : (
                                                        <span>Pick a date</span>
                                                    )}
                                                    <CalendarIcon className="ml-auto h-4 w-4 opacity-50" />
                                                </Button>
                                            </FormControl>
                                        </PopoverTrigger>
                                        <PopoverContent className="w-auto p-0" align="start">
                                            <Calendar
                                                mode="single"
                                                selected={field.value || undefined}
                                                onSelect={(date) => setValue("closeDate", date ?? null)}
                                                disabled={(date) =>
                                                    date < new Date("1900-01-01")
                                                }
                                                initialFocus
                                                data-qa-id="calendar-closeDate"
                                            />
                                        </PopoverContent>
                                    </Popover>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>
                </div>
            </form>
        </Form>
    </div>
    );
};

export default GeneralInfoTab;
