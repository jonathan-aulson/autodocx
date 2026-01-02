import { Reducer } from "redux";
// import { MaxProfitPerDay } from "../../../interface/Currency";
import { MaxProfitPerDayActions, MaxProfitPerDayActionTypes } from "./action";

export interface MaxProfitPerDayState {
	readonly maxProfitPerDay: object;
}

const initialMaxProfitPerDayState: MaxProfitPerDayState = {
	maxProfitPerDay: {}
};

export const maxProfitPerDayReducer: Reducer<
	MaxProfitPerDayState,
	MaxProfitPerDayActions
> = (state = initialMaxProfitPerDayState, action) => {
	switch (action.type) {
		case MaxProfitPerDayActionTypes.GET_ALL: {
			if (typeof action.maxProfitPerDay.currency !== "undefined") {
				return {
					...state,
					maxProfitPerDay: {
						...state.maxProfitPerDay,
						[action.maxProfitPerDay.currency]: action.maxProfitPerDay
					}
				};
			} else{
				return state;
			}
		}
		default:
			return state;
	}
};
