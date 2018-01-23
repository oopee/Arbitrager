import { Action, Reducer } from 'redux';
import { fetch, addTask } from 'domain-task';
import { AppThunkAction } from './';

// -----------------
// STATE - This defines the type of data maintained in the Redux store.

export interface ArbitrageState {
    readonly isLoading: boolean;
    readonly eurAmount: number;
    readonly infoData: ArbitrageInfoResponse;
    readonly executeData: any;
    readonly states: ArbitrageContext[];
    readonly mode: ArbitrageMode;
    readonly automaticArbitragerRunning: boolean;
}

// -----------------
// ACTIONS - These are serializable (hence replayable) descriptions of state transitions.
// They do not themselves have any side-effects; they just describe something that is going to happen.

interface RequestArbitrageInfoAction {
    type: 'REQUEST_ARBITRAGE_INFO';
    eurAmount: number;
}

interface ReceiveArbitrageInfoAction {
    type: 'RECEIVE_ARBITRAGE_INFO';
    data: ArbitrageInfoResponse;
}

interface RequestArbitrageExecuteAction {
    type: 'REQUEST_ARBITRAGE_EXECUTE';
    eurAmount: number;
}

interface ReceiveArbitrageExecuteAction {
    type: 'RECEIVE_ARBITRAGE_EXECUTE';
    data: ArbitrageInfoResponse;
}

interface SetEurAmountAction {
    type: 'SET_EUR_AMOUNT';
    eurAmount: number;
}

interface SetArbitragerModeAction {
    type: 'SET_ARBITRAGER_MODE';
    mode: ArbitrageMode;
}

interface RequestAutomaticArbitragerAction {
    type: 'REQUEST_AUTOMATIC_ARBITRAGER';
    run: boolean;
}

interface ReceiveAutomaticArbitragerAction {
    type: 'RECEIVE_AUTOMATIC_ARBITRAGER';
    isRunning: boolean;
}

export enum ArbitrageMode {
    Manual = "Manual",
    Automatic = "Automatic",
}

export interface ChangeArbitragerStateAction {
    type: 'CHANGE_ARBITRAGER_STATE';
    data: ArbitrageContext;
}

export interface ArbitrageContext {
    baseAsset: string;
    quoteAsset: string;
    state: number;
    stateName: string;
    userDefinedQuoteCurrencyToSpend: number;
    buyOrder_QuoteCurrencyLimitPriceToUse : number;
    buyOrder_BaseCurrencyAmountToBuy: number;
    error: string;
    buyBaseCurrencyAmount: number;
    buyOrderId: string;
    sellOrderId: string;
    buyOrder: Order;
    sellOrder: Order;
    buyerName: string;
    sellerName: string;
    finishedResult: ArbitrageContextFinishedResult;
    info: ArbitrageInfoResponse;
}

interface ArbitrageContextFinishedResult {
    baseCurrencyBought: number;
    baseCurrencySold: number;
    baseCurrencyDelta: number;
    quoteCurrencySpent: number;
    quoteCurrencyEarned: number;
    quoteCurrencyDelta: number;
    profitPercentage: number;
    buyerBalance: Balance;
    sellerBalance: Balance;
}

interface Order {
    id: string;
    side: string;
    state: string;
    type: string;
    volume: number;
    filledVolume: number;
    limitPrice: number;
    fee: number;
    costExcludingFee: number;
    costIncludingFee: number;
    openTime: Date;
    closeTime: Date;
    expireTime: Date;
    averageUnitPrice: number;
}

interface ArbitrageInfoResponse {
    baseAsset: string;
    quoteAsset: string;
    maxNegativeSpread: number;
    maxNegativeSpreadPercentage: number;
    buyerName: string;
    sellerName: string;
    bestBuyPrice: number;
    bestSellPrice: number;
    estimatedAvgBuyUnitPrice: number;
    estimatedAvgNegativeSpread: number;
    estimatedAvgNegativeSpreadPercentage: number;
    estimatedAvgSellUnitPrice: number;
    baseCurrencyBalance: number;
    quoteCurrencyBalance: number;
    isBaseBalanceSufficient: boolean;
    isQuoteBalanceSufficient: boolean;
    isProfitable: boolean;

    profitCalculation: ProfitCalculation;
    buyer: ExchangeStatus;
    seller: ExchangeStatus;
}

interface ProfitCalculation {
    quoteCurrencyEarned: number;
    quoteCurrencySpent: number;
    baseCurrencyBuyCount: number;
    baseCurrencySellCount: string;
    buyLimitPriceUnit: string;
    profit: number;
    profitAfterTax: number;
    profitPercentage: number;    
    buyFee: number;
    sellFee: number;
    allQuoteCurrencySpent: boolean;
}

interface ExchangeStatus
{
    name: string;
    balance: Balance;
    takerFee: number;
    makerFee: number;
}

interface Balance {
    all: any;
    base: number;
    quote: number;
    baseAsset: string;
    quoteAsset: string;
}

// Declare a 'discriminated union' type. This guarantees that all references to 'type' properties contain one of the
// declared type strings (and not any other arbitrary string).
type KnownAction = RequestArbitrageInfoAction
    | ReceiveArbitrageInfoAction
    | RequestArbitrageExecuteAction
    | ReceiveArbitrageExecuteAction
    | SetEurAmountAction
    | ChangeArbitragerStateAction
    | RequestAutomaticArbitragerAction
    | ReceiveAutomaticArbitragerAction
    | SetArbitragerModeAction;

// ----------------
// ACTION CREATORS - These are functions exposed to UI components that will trigger a state transition.
// They don't directly mutate state, but they can have external side-effects (such as loading data).

export const actionCreators = {
    requestArbitrageInfo: (eurAmount: number): AppThunkAction<KnownAction> => (dispatch, getState) => {
        let fetchTask = fetch(`api/arbitrage/info?amount=${ eurAmount }`)
            .then(response => response.json() as Promise<ArbitrageInfoResponse>)
            .then(data => {
                dispatch({
                    type: 'RECEIVE_ARBITRAGE_INFO',
                    data,
                });
            });

        addTask(fetchTask); // Ensure server-side prerendering waits for this to complete
        dispatch({ type: 'REQUEST_ARBITRAGE_INFO', eurAmount });
    },
    requestExecuteArbitrage: (eurAmount: number): AppThunkAction<KnownAction> => (dispatch, getState) => {
        let fetchTask = fetch(`api/arbitrage/execute?amount=${ eurAmount }`)
            .then(response => response.json() as Promise<ArbitrageInfoResponse>)
            .then(data => {
                dispatch({
                    type: 'RECEIVE_ARBITRAGE_EXECUTE',
                    data,
                });
            });

        addTask(fetchTask); // Ensure server-side prerendering waits for this to complete
        dispatch({ type: 'REQUEST_ARBITRAGE_EXECUTE', eurAmount });
    },
    requestAutomaticArbitrage: (run: boolean): AppThunkAction<KnownAction> => (dispatch, getState) => {
        let fetchTask = fetch(`api/arbitrage/auto?run=${ run }`)
            .then(response => response.json() as boolean)
            .then(data => {
                dispatch({
                    type: 'RECEIVE_AUTOMATIC_ARBITRAGER',
                    isRunning: data,
                });
            });

        addTask(fetchTask); // Ensure server-side prerendering waits for this to complete
        dispatch({ type: 'REQUEST_AUTOMATIC_ARBITRAGER', run });
    },
    setEurAmount: (eurAmount: number): AppThunkAction<KnownAction> => (dispatch, getState) => {
        dispatch({ type: 'SET_EUR_AMOUNT', eurAmount });
    },
    setArbitrageMode: (mode: ArbitrageMode): AppThunkAction<KnownAction> => (dispatch, getState) => {
        dispatch({ type: 'SET_ARBITRAGER_MODE', mode });
    }
};

// ----------------
// REDUCER - For a given state and action, returns the new state. To support time travel, this must not mutate the old state.

const defaultState: ArbitrageState = {
    isLoading: false,
    eurAmount: 100,
    infoData: <ArbitrageInfoResponse>{
        profitCalculation: <ProfitCalculation>{},
        buyer: <ExchangeStatus>{
            balance: <Balance>{},
        },
        seller: {
            balance: <Balance>{},
        }
    },
    executeData: {},
    states: [],
    mode: ArbitrageMode.Manual,
    automaticArbitragerRunning: false,
};

export const reducer: Reducer<ArbitrageState> = (state: ArbitrageState, incomingAction: Action) => {
    const action = incomingAction as KnownAction;
    switch (action.type) {
        case 'REQUEST_ARBITRAGE_INFO':
            return {
                ...state,
                isLoading: true,
            };
        case 'RECEIVE_ARBITRAGE_INFO':
            return {
                ...state,
                isLoading: false,
                infoData: action.data,
            };
        case 'REQUEST_ARBITRAGE_EXECUTE':
            return {
                ...state,
                isLoading: true,
            };
        case 'RECEIVE_ARBITRAGE_EXECUTE':
            return {
                ...state,
                isLoading: false,
                executeData: action.data,                
            };            
        case 'SET_EUR_AMOUNT':
            return {
                ...state,
                eurAmount: action.eurAmount,
            };
        case 'REQUEST_AUTOMATIC_ARBITRAGER':
            return {
                ...state,
            };
        case 'RECEIVE_AUTOMATIC_ARBITRAGER':
            return {
                ...state,
                automaticArbitragerRunning: action.isRunning,
            };
        case 'CHANGE_ARBITRAGER_STATE':
            return {
                ...state,
                states: [action.data, ...state.states],
                infoData: action.data.info ? action.data.info : state.infoData, // update info if it was received
            };
        case 'SET_ARBITRAGER_MODE':
            return {
                ...state,
                mode: action.mode,
            };
        default:
            // The following line guarantees that every action in the KnownAction union has been covered by a case above
            const exhaustiveCheck: never = action;
    }

    return state || defaultState;
};
