"use strict";
import { allowedCurrencies } from "../config";
import { maxProfitPerDay } from "./lib/currency";
export const calculateProfit = async (event, _context) => {
    if (event.queryStringParameters == null ||
        typeof event.queryStringParameters.currency === "undefined" ||
        (typeof event.queryStringParameters.currency !== "undefined" &&
            !allowedCurrencies.includes(event.queryStringParameters.currency.toUpperCase()))) {
        return {
            statusCode: 400,
            body: JSON.stringify({
                message: "Invalid request found."
            })
        };
    }
    const requestCurrency = event.queryStringParameters.currency.toUpperCase();
    const profitData = maxProfitPerDay(requestCurrency);
    return {
        statusCode: 200,
        body: JSON.stringify(profitData)
    };
};
//# sourceMappingURL=currencyExchange.js.map