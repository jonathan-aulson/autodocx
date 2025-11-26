import { faker } from "@faker-js/faker";

export default class FakerHelper {
  static generateGeneralInfoData() {
    // Generate random start and close dates
    const startDate = faker.date.past(1); // Generates a start date within the past year
    const closeDate = faker.date.between(startDate, new Date()); // Generates a close date after the start date
    
    return {
      ACCOUNTMANAGER: faker.name.fullName(), // Generates a full name
      ASSISTANTACCOUNTMANAGER: faker.name.fullName(),
      DISTRICTMANAGER: faker.name.fullName(),
      ASSISTANTDISTRICTMANAGER: faker.name.fullName(),
      SITENAME: faker.company.name(), // Generates a company name
      INVOICERECIPIENT: faker.name.fullName(),
      BILLINGCONTACTEMAIL: faker.internet.email(), // Generates an email address
      GLSTRING: `${faker.datatype.number({ min: 1, max: 99 }).toString().padStart(2, '0')}-${faker.datatype.number({ min: 1, max: 99 }).toString().padStart(2, '0')}-${faker.datatype.number({ min: 1000, max: 9999 }).toString()}-`, // GL string format 'XX-XX-XXXX-'
      DISTRICT: `D - ${faker.address.city()}`, // District format 'D - City'
      ADDRESS: faker.address.streetAddress(), // Generates an address
      ACCOUNTMANAGERID: faker.datatype.number({ min: 1000000, max: 9999999 }).toString(), // Generates a 7-digit account manager ID
      VENDORID: faker.datatype.number({ min: 1000, max: 9999 }).toString(),
      TOTALROOMSAVAILABLE: faker.datatype.number({ min: 1000, max: 9999 }).toString(),
      TOTALAVAILABLEPARKING: faker.datatype.number({ min: 1000000, max: 9999999 }).toString(),
      STARTDATE: startDate.toISOString().split('T')[0], // Converts start date to 'YYYY-MM-DD' format
      CLOSEDATE: closeDate.toISOString().split('T')[0]  // Converts close date to 'YYYY-MM-DD' format
    };
  }
  
  static generateInvalidEmail() {
    return faker.lorem.word() + "@invalid";
  }
  static generateGeneralSetupData() {
    return{
      CONTRACTTYPE: faker.commerce.department(), // Generates a random contract type (you can adjust if needed)
      PONUMBER: faker.datatype.uuid(), // Generates a PO number
      DEVIATIONPERCENTAGE: faker.datatype.float({ min: 0, max: 100, precision: 0.01 }), // Generates a deviation percentage
      DEVIATIONAMOUNT: faker.finance.amount(), // Generates a deviation amount
      INCREMENTPERCENTAGE: faker.datatype.float({ min: 0, max: 100, precision: 0.01 }), // Generates an increment percentage
      NOTES: faker.lorem.sentence(),
      DETAILS: faker.lorem.word()
    }
     
  }
  static generateRevenueReviewThresholdData() {
    return{
      DEVIATIONPERCENTAGE: faker.datatype.float({ min: 0, max: 100, precision: 0.01 }), // Generates a deviation percentage
      DEVIATIONAMOUNT: faker.finance.amount(), // Generates a deviation amount
      
    }
     
  }
  
  static generateMultipleInvoiceData() {
    return{
      VALUETOENTER: faker.commerce.productName()
    }
  }
  static generatePerLaborHourData() {
    return{
      JOBCODE: faker.random.alphaNumeric(5)
    }
  }

  static generateSiteIDNumber() {
    return {
        SITE_ID: faker.datatype.number({ min: 1000, max: 9999 }).toString() // Convert to string if needed
    };
}

}
