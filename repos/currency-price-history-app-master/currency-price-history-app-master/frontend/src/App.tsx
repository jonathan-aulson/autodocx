import React from "react";
import "./App.css";
import MaxProfitPerDayList from "./pages/profit/index";
import { CURRENCIES } from "./config";

const App: React.FC = () => {
	let currencyElements = [];
	for (let i = 0; i < CURRENCIES.length; i++) {
		currencyElements.push(
			<MaxProfitPerDayList key={i} currency={CURRENCIES[i]} />
		);
	}

	return <React.Fragment>{currencyElements}</React.Fragment>;
};

export default App;
