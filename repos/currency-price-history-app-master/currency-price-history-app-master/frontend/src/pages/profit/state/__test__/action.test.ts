import configureStore from 'redux-mock-store';
import thunk from 'redux-thunk';
import { bindActionCreators } from 'redux';
import { getAllMaxProfitPerDay, MaxProfitPerDayActionTypes } from '../action';
import moxios from 'moxios';

const middlewares = [thunk];
const mockStore = configureStore(middlewares);
const mockData = {
  "currency": "LTC",
  "date": "20180507",
  "buy": {
      "time": "0930",
      "price": 14.32
  },
  "sell": {
      "time": "1245",
      "price": 15.03
  },
  "max_profit": 0.71
};

describe('getAllMaxProfitPerDay Action Creator', () => {
  beforeEach(() => {
    moxios.install();
  });

  afterEach(() => {
    moxios.uninstall();
  });

  it('dispatches expected actions when succesffuly fetching requisition list', async () => {
    moxios.wait(() => {
      const request = moxios.requests.mostRecent();
      request.respondWith({
        response: mockData,
      });
    });

    const expectedActions = [
      { type: MaxProfitPerDayActionTypes.GET_ALL, maxProfitPerDay: mockData },
    ];

    const store = mockStore();

    const getAllMaxProfitPerDayAction = bindActionCreators(getAllMaxProfitPerDay, store.dispatch);
    await getAllMaxProfitPerDayAction();
    expect(store.getActions()).toEqual(expectedActions);
  });
});

