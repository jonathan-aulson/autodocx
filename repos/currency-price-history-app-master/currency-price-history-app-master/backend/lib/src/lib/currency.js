"use strict";
import * as _ from "lodash";
import currencyData from "../../data/currency.json";
import { convertStringToDate } from "./datetime";
// export function getCurrencyData(): Promise<CurrencyData[]> {
//   return new Promise((resolve, reject) => {
//     axios({
//       'get',
//       process.env.CURRENCY_DATA_URL,
//       headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' }
//     }).then((response: any) => {
//       resolve(response.data);
//     }).
//     catch((error: any) => {
//       reject(error);
//     });
//   });
// } 
export function findByCurrency(currency) {
    return _.filter(currencyData, { currency: currency })[0];
}
export function maxProfitPerDay(currency) {
    let response = {
        currency: "",
        date: "",
        buy: {
            time: "",
            price: ""
        },
        sell: {
            time: "",
            price: ""
        },
        max_profit: 0
    };
    const data = findByCurrency(currency);
    if (typeof data === "undefined") {
        return response;
    }
    _.forEach(data.quotes, function (a, aKey) {
        a["price"] = parseFloat(a["price"]);
        const aDatetime = convertStringToDate(data.date + a["time"], "YYYYMMDDHHmm", "YYYY-MM-DD HH:mm");
        _.forEach(data.quotes, function (b, bKey) {
            if (aKey == bKey) {
                return;
            }
            const bDatetime = convertStringToDate(data.date + b["time"], "YYYYMMDDHHmm", "YYYY-MM-DD HH:mm");
            b["price"] = parseFloat(b["price"]);
            if (aDatetime < bDatetime && b["price"] > a["price"]) {
                let profit = b["price"] - a["price"];
                if (profit > response["max_profit"]) {
                    response["currency"] = data.currency;
                    response["date"] = data.date;
                    response["buy"]["time"] = a["time"];
                    response["buy"]["price"] = a["price"];
                    response["sell"]["time"] = b["time"];
                    response["sell"]["price"] = b["price"];
                    response["max_profit"] = Math.ceil(profit * 100) / 100;
                }
            }
        });
    });
    return response;
}
//# sourceMappingURL=currency.js.map