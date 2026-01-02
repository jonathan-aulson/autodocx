import * as React from "react";
import * as renderer from "react-test-renderer";
import thunk from "redux-thunk";
import { Provider } from "react-redux";
import MaxProfitPerDayList from "../index";
import configureMockStore from "redux-mock-store";

const middlewares = [thunk];
const mockStore = configureMockStore(middlewares);

describe("MaxProfitPerDayList component", () => {
	let defaultProps = {
		maxProfitPerDayState: {
			maxProfitPerDay: {
				ETC: {
					currency: "ETC",
					date: "20180507",
					buy: {
						time: "0930",
						price: 14.32
					},
					sell: {
						time: "1245",
						price: 15.03
					},
					max_profit: 0.71
				}
			}
		}
	};

	it("renders currency profit page", () => {
		const store = mockStore(defaultProps);

		const renderedMaxProfitPerDayList = renderer
			.create(
				<Provider store={store}>
					<MaxProfitPerDayList currency="ETC" />
				</Provider>
			)
			.toJSON();
		expect(renderedMaxProfitPerDayList).toMatchSnapshot();
	});
});
