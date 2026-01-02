import { expect } from "chai";
import * as sinon from 'sinon';
import { calculateProfit } from "../../src/currencyExchange";
import * as currency from "../../src/lib/currency";

describe("calculateProfit()", function asyncConcatTest() {
	let maxProfitPerDayStub;

	const result = {
		currency: "BTC",
		date: "07-May-18",
		buy: {
			time: "09:15 AM",
			price: 34.98
		},
		sell: {
			time: "12:30 PM",
			price: 37.01
		},
		max_profit: 2.04
	};

	context("with valid arguments", function() {
		before(function beforeTest() {
			maxProfitPerDayStub = sinon.stub(currency, 'maxProfitPerDay');
			maxProfitPerDayStub.withArgs('BTC').callsFake(function() {
				return Promise.resolve(result);
			});
		});

		it("should return 200 response", async () => {
			const event = { queryStringParameters: { currency: 'BTC' } };
			const response: any = await calculateProfit(<any>event, <any>{}, <any>{});
			expect(response.statusCode).to.equal(200);
		});

		after(function afterTest() {
			maxProfitPerDayStub.restore();
		});
	});

	context("with invalid arguments", function() {
		before(function beforeTest() {
			maxProfitPerDayStub = sinon.stub(currency, 'maxProfitPerDay');
			maxProfitPerDayStub.withArgs('BTC').callsFake(function() {
				return Promise.resolve(result);
			});
		});

		it("should return 400 response", async () => {
			const event = { queryStringParameters: { currency: 'INVALID_CURRENCY_CODE' } };
			const response: any = await calculateProfit(<any>event, <any>{}, <any>{});
			expect(response.statusCode).to.equal(400);
		});

		after(function afterTest() {
			maxProfitPerDayStub.restore();
		});

	});
});
