import * as calculateProfit from "../src/currencyExchange";
import { expect } from "chai";
import { sinon } from "sinon";
import { maxProfitPerDay } from "../src/lib/currency";

describe("asyncConcat", function asyncConcatTest() {
	let maxProfitPerDayStub;
	const result = {
		currency: "BTC",
		date: "20180507",
		buy: {
			time: "0915",
			price: 34.98
		},
		sell: {
			time: "1230",
			price: 37.01
		},
		max_profit: 2.04
	};
	context("input ok", function() {
		const queryStringParameters = { currency: "BTC" };
		before(function beforeTest() {
			maxProfitPerDayStub = sinon.stub(maxProfitPerDay);
			maxProfitPerDayStub.callsFake(function(queryCurrency) {
				expect(queryCurrency).to.eq("BTC");
				return Promise.resolve(result);
			});
		});
		it("calculateProfit - with valid response", async () => {
			const event = { queryStringParameters };
			const response = await calculateProfit(event, {}, {});
			expect(response.statusCode).to.equal(200);
		});
		after(function afterTest() {
			maxProfitPerDayStub.restore();
		});
	});
});
//# sourceMappingURL=calculateProfit.test.js.map
