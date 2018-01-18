import * as React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { connect } from 'react-redux';
import { ApplicationState }  from '../store';
import * as ArbitrageState from '../store/Arbitrage';

// At runtime, Redux will merge together...
type ArbitrageProps =
    ArbitrageState.ArbitrageState           // ... state we've requested from the Redux store
    & typeof ArbitrageState.actionCreators  // ... plus action creators we've requested
    & RouteComponentProps<{}>;              // ... plus incoming routing parameters

class Arbitrage extends React.Component<ArbitrageProps, {}> {
    componentWillMount() {
        this.getArbitrageInfo();
    }

    getArbitrageInfo() {
        this.props.requestArbitrageInfo(this.props.eurAmount);
    }

    executeArbitrage() {
        //this.props.requestArbitrageInfo(this.props.eurAmount); // to update data visible to match command
        this.props.requestExecuteArbitrage(this.props.eurAmount);
    }
    
    toggleAutomaticArbitrager() {
        let run = !this.props.automaticArbitragerRunning;
        this.props.requestAutomaticArbitrage(run);
    }

    public render() {
        return <div>
            <h1>Arbitrage</h1>
            
            <input value={ this.props.eurAmount } onChange={ (event) => { this.props.setEurAmount(parseFloat(event.target.value)) } } />
            <span>€</span>
            <button onClick={ () => { this.getArbitrageInfo() } }>Get info</button>
            <button onClick={ () => { this.executeArbitrage() } }>Execute</button>
            <button onClick={ () => { this.toggleAutomaticArbitrager() } }>{ this.props.automaticArbitragerRunning ? "Stop" : "Start" } automatic arbitrager</button>
            { this.props.isLoading ? <span>Loading...</span> : [] }
            
            <div className='container-fluid'>
                <div className='row'>
                    <div className='col-sm-5'>
                        <p className='title'>Basic information</p>
                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Max negative spread (€)</td>
                                    <td>{ this.props.infoData.maxNegativeSpreadEur } €</td>
                                </tr>
                                <tr>
                                    <td>Max negative spread (%)</td>
                                    <td>{ this.props.infoData.maxNegativeSpreadPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>Buyer implementation</td>
                                    <td>{ this.props.infoData.buyerName }</td>
                                </tr>
                                <tr>
                                    <td>Seller implementation</td>
                                    <td>{ this.props.infoData.sellerName }</td>
                                </tr>
                                <tr>
                                    <td>Estimated average buy unit price</td>
                                    <td>{ this.props.infoData.estimatedAvgBuyUnitPrice } €</td>
                                </tr>
                                <tr>
                                    <td>Estimated average sell unit price</td>
                                    <td>{ this.props.infoData.estimatedAvgSellUnitPrice } €</td>
                                </tr>
                                <tr>
                                    <td>Estimated negative spread (€)</td>
                                    <td>{ this.props.infoData.estimatedAvgNegativeSpread } €</td>
                                </tr>
                                <tr>
                                    <td>Estimated negative spread (%)</td>
                                    <td>{ this.props.infoData.estimatedAvgNegativeSpreadPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>ETH balance sufficient</td>
                                    <td>{ this.props.infoData.isEthBalanceSufficient ? 'true' : 'false' }</td>
                                </tr>
                                <tr>
                                    <td>EUR balance sufficient</td>
                                    <td>{ this.props.infoData.isEurBalanceSufficient ? 'true' : 'false' }</td>
                                </tr>
                                <tr>
                                    <td>Profitable</td>
                                    <td>{ this.props.infoData.isProfitable ? 'true' : 'false' }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    
                    <div className='col-sm-5'>
                        <p className='title'>Profit calculation</p>

                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Fiat spent (€)</td>
                                    <td>{ this.props.infoData.profitCalculation.fiatSpent } €</td>
                                </tr>
                                <tr>
                                    <td>Fiat earned</td>
                                    <td>{ this.props.infoData.profitCalculation.fiatEarned  } €</td>
                                </tr>
                                <tr>
                                    <td>Profit (€)</td>
                                    <td>{ this.props.infoData.profitCalculation.profit } €</td>
                                </tr>
                                <tr>
                                    <td>Profit (%)</td>
                                    <td>{ this.props.infoData.profitCalculation.profitPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>Profit after tax (€)</td>
                                    <td>{ this.props.infoData.profitCalculation.profitAfterTax } €</td>
                                </tr>
                                <tr>
                                    <td>ETH buy count</td>
                                    <td>{ this.props.infoData.profitCalculation.ethBuyCount }</td>
                                </tr>
                                <tr>
                                    <td>ETH sell count</td>
                                    <td>{ this.props.infoData.profitCalculation.ethSellCount }</td>
                                </tr>
                                <tr>
                                    <td>Buy fee (€)</td>
                                    <td>{ this.props.infoData.profitCalculation.buyFee }</td>
                                </tr>
                                <tr>
                                    <td>Sell fee (€)</td>
                                    <td>{ this.props.infoData.profitCalculation.sellFee }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div className='col-sm-2'>
                        <p className='title'>Status</p>

                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Buyer balance EUR</td>
                                    <td>{ this.props.infoData.buyer.balance.eur } €</td>
                                </tr>
                                <tr>
                                    <td>Buyer balance ETH</td>
                                    <td>{ this.props.infoData.buyer.balance.eth } ETH</td>
                                </tr>
                                <tr>
                                    <td>Seller balance EUR</td>
                                    <td>{ this.props.infoData.seller.balance.eur } €</td>
                                </tr>
                                <tr>
                                    <td>Seller balance EUR</td>
                                    <td>{ this.props.infoData.seller.balance.eth } ETH</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className='row'>
                    <div className='col-sm-12'>
                        <p className='title'>State changes</p>

                        <table className='table'>
                            <tbody>
                                { this.props.states.map((state, index) => {
                                    return this.renderState(state, index)                                    
                                }) }
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>;
    }

    private renderState(state: ArbitrageState.ArbitrageContext, index: number) {
        interface KVP {
            key: string;
            value: string;
        }

        let properties = [] as KVP[];

        if (state.stateName == "CheckStatus") {            
            properties.push({ key: "EthAmountToBuy", value: state.buyOrder_EthAmountToBuy.toString() });
            properties.push({ key: "LimitPriceToUse", value: state.buyOrder_LimitPriceToUse.toString() });
        }
        else if (state.stateName == "PlaceBuyOrder") {
            properties.push({ key: "BuyOrderId", value: state.buyOrderId });
        }
        else if (state.stateName == "GetBuyOrderInfo") {
            properties.push({ key: "Limit price", value: state.buyOrder.limitPrice.toString() });
            properties.push({ key: "Volume", value: state.buyOrder.volume.toString() });
            properties.push({ key: "Filled volume", value: state.buyOrder.filledVolume.toString() });
            properties.push({ key: "Cost (including fee)", value: state.buyOrder.costIncludingFee.toString() });
            properties.push({ key: "Fee", value: state.buyOrder.fee.toString() });
            properties.push({ key: "Open time", value: state.buyOrder.openTime.toString() });
            properties.push({ key: "Close time", value: state.buyOrder.closeTime.toString() });
            properties.push({ key: "Exchange", value: state.buyerName });
        }
        else if (state.stateName == "PlaceSellOrder") {
            properties.push({ key: "SellOrderId", value: state.sellOrderId });
        }
        else if (state.stateName == "GetSellOrderInfo") {
            properties.push({ key: "Limit price", value: state.sellOrder.limitPrice.toString() });
            properties.push({ key: "Volume", value: state.sellOrder.volume.toString() });
            properties.push({ key: "Filled volume", value: state.sellOrder.filledVolume.toString() });
            properties.push({ key: "Cost (including fee)", value: state.sellOrder.costIncludingFee.toString() });
            properties.push({ key: "Fee", value: state.sellOrder.fee.toString() });
            properties.push({ key: "Open time", value: state.sellOrder.openTime.toString() });
            properties.push({ key: "Close time", value: state.sellOrder.closeTime.toString() });
            properties.push({ key: "Exchange", value: state.sellerName });
        }
        else if (state.stateName == "CalculateFinalResult") {
            properties.push({ key: "ETH bought", value: state.finishedResult.ethBought.toString() });
            properties.push({ key: "ETH sold", value: state.finishedResult.ethSold.toString() });
            properties.push({ key: "ETH delta", value: state.finishedResult.ethDelta.toString() });
            properties.push({ key: "Fiat spent", value: state.finishedResult.fiatSpent.toString() });
            properties.push({ key: "Fiat earned", value: state.finishedResult.fiatEarned.toString() });
            properties.push({ key: "Fiat delta", value: state.finishedResult.fiatDelta.toString() });
            properties.push({ key: "Profit", value: state.finishedResult.profitPercentage.toString() + " %" });
        }

        if (state.error) {
            properties.push({ key: "Error", value: state.error });
        }

        return <tr key={ index }>
            <td>
                { state.stateName }
            </td>
            <td>
                { properties.map((col, index) => {
                    return <p key={ index }>
                        { col.key } : { col.value }
                    </p>
                })}
            </td>
        </tr>
    }
}

export default connect(
    (state: ApplicationState) => state.arbitrage,   // Selects which state properties are merged into the component's props
    ArbitrageState.actionCreators                   // Selects which action creators are merged into the component's props
)(Arbitrage) as typeof Arbitrage;
