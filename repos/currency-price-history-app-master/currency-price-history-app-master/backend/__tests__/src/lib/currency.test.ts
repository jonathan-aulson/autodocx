import { expect } from "chai";
import * as sinon from 'sinon';
import * as currency from "../../../src/lib/currency";

describe("maxProfitPerDay()", function asyncConcatTest() {
	let findByCurrencyStub;
	const skelitonResponse = {
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

	context("currency code with quotes data", function() {
		before(function beforeTest() {
      process.env.CURRENCY_DATA_URL = 'https://prabhath-currency-data.s3-ap-southeast-2.amazonaws.com/currency.json';
			findByCurrencyStub = sinon.stub(currency, 'findByCurrency');
			findByCurrencyStub.withArgs('BTC').callsFake(function() {
				return Promise.resolve(result);
			});
		});

		it("should return calculated data", async () => {
			const response: any = await currency.maxProfitPerDay(<any>'BTC');
			expect(response).to.eql(result);
		});

		after(function afterTest() {
			findByCurrencyStub.restore();
		});
	});

	context("currency code with no quotes data", function() {
		before(function beforeTest() {
			findByCurrencyStub = sinon.stub(currency, 'findByCurrency');
			findByCurrencyStub.withArgs('BTC').callsFake(function() {
				return Promise.resolve(result);
			});
		});

		it("should return skeliton of data", async () => {
			const response: any = await currency.maxProfitPerDay(<any>'INVALID_CURRENCY_CODE');
			expect(response).to.eql(skelitonResponse);
		});

		after(function afterTest() {
      delete process.env.CURRENCY_DATA_URL;
			findByCurrencyStub.restore();
		});

	});
});
