import { applyMiddleware, combineReducers, createStore, Store } from "redux";
import thunk from "redux-thunk";
import {
	maxProfitPerDayReducer,
	MaxProfitPerDayState
} from "../pages/profit/state/reducer";

export interface AppState {
	maxProfitPerDayState: MaxProfitPerDayState;
}

const rootReducer = combineReducers<AppState>({
	maxProfitPerDayState: maxProfitPerDayReducer
});

export default function configureStore(): Store<AppState, any> {
	const store = createStore(rootReducer, undefined, applyMiddleware(thunk));
	return store;
}
