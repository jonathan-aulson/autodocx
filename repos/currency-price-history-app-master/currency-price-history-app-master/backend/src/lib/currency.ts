"use strict";
import * as _ from "lodash";
import * as moment from "moment";
import { CurrencyData, Quote } from "../interface/Currency";
import { convertStringToDate } from "./datetime";
import { getRequest } from "./api";

export interface MaxProfitPerDayResponse {
	currency: string;
	date: string;
	buy: Quote;
	sell: Quote;
	max_profit: number;
}

export async function getCurrencyData(): Promise<CurrencyData[]|{}> {
	return await getRequest(process.env.CURRENCY_DATA_URL);
}

export async function findByCurrency(currency: string): Promise<CurrencyData> {
	let response = null;
	const currencyData = await getCurrencyData();
	
	if(Array.isArray(currencyData)) {
		response = _.filter(currencyData, { currency: currency })[0];
	}
	
	return response;
}

export async function maxProfitPerDay(
	currency: string
): Promise<MaxProfitPerDayResponse> {

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

	const data = await findByCurrency(currency);

	if (typeof data === "undefined" ) {
		return response;
	}

	_.forEach(data.quotes, function(a, aKey) {
		a["price"] = parseFloat(a["price"]);
		const aDatetime = convertStringToDate(
			data.date + a["time"],
			"YYYYMMDDHHmm",
			"YYYY-MM-DD HH:mm"
		);
		_.forEach(data.quotes, function(b, bKey) {
			if (aKey == bKey) {
				return;
			}
			const bDatetime = convertStringToDate(
				data.date + b["time"],
				"YYYYMMDDHHmm",
				"YYYY-MM-DD HH:mm"
			);

			b["price"] = parseFloat(b["price"]);

			if (aDatetime < bDatetime && b["price"] > a["price"]) {
				let profit = b["price"] - a["price"];
				if (profit > response["max_profit"]) {
					response["currency"] = data.currency;
					response["date"] = moment(data.date, 'YYYYMMDDHHmm').format('DD-MMMM-YY');
					response["buy"]["time"] = moment(aDatetime).format('hh:mm A');
					response["buy"]["price"] = a["price"];
					response["sell"]["time"] = moment(bDatetime).format('hh:mm A');
					response["sell"]["price"] = b["price"];
					response["max_profit"] = Math.ceil(profit * 100) / 100;
				}
			}
		});
	});

	return response;
}
