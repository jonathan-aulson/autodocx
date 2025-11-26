const generatedValue =  "Search"+ (Math.random().toString(36).slice(2,10));
export default class Helper{

    static savedName = Helper.generateName()
    static savedDescription =  Helper.generateDescription()

    static generateName(){
        return "Auto_Test-"+ (Math.random().toString(36).slice(2,7)).toLocaleUpperCase();
    }

    static generateDescription(){
        return "Auto_Test-"+ (Math.random().toString(36).slice(2,7)).toLocaleUpperCase();
    }

    static getName() {
        return this.savedName;
    }

    static getDescription() {
        return this.savedDescription;
    }

    static generteNumber(){
        return `${Cypress._.random(0, 1e5)}`;
    }

     // Generate a random value only once

    static searchRandom() {
        return generatedValue;
    }

}