import * as SignalR from '@aspnet/signalr-client';
import * as ArbitrageState from './store/Arbitrage';

const connection = new SignalR.HubConnection('http://localhost:5000/signalr.arbitrager');

export function init() {
    connection.start().then(() => {
        console.log("WebSocket connected");
    });
    connection.onclose(() => {
        console.log("WebSocket disconnected");
    });
}

export function middleware(store: any) {
    return (next: any) => async (action: any) => {
        switch (action.type) {
        case "SERVER_ACTION_TEST":
            connection.invoke('TestAction');
            break;
        }
        return next(action);
    }
}

export function registerHandlers(store: any) {
    connection.on('StateChanged', data => {
        const typedData = data as ArbitrageState.ArbitrageContext;
        store.dispatch(<ArbitrageState.ChangeArbitragerStateAction>{type: 'CHANGE_ARBITRAGER_STATE', data: typedData })
    })
}
