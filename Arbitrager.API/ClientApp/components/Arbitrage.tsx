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
        this.props.requestExecuteArbitrage(this.props.eurAmount);
    }

    public render() {
        return <div>
            <h1>Arbitrage</h1>
            
            <input value={ this.props.eurAmount } onChange={ (event) => { this.props.setEurAmount(parseFloat(event.target.value)) } } />
            <span>€</span>
            <button onClick={ () => { this.getArbitrageInfo() } }>Get info</button>            
            <button onClick={ () => { this.executeArbitrage() } }>Execute</button>
            
            <div className='container-fluid'>
                <div className='row'>
                    <div className='col-sm-5'>
                        { this.props.isLoading ? <span>Loading...</span> : [] }

                        <p className='title'>Basic information</p>
                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Max negative spread (€)</td>
                                    <td>{ this.props.infoData.maxNegativeSpreadEur } €</td>
                                </tr>
                                <tr>
                                    <td>Max negative spread (%)</td>
                                    <td>{ (this.props.infoData.maxNegativeSpreadPercentage * 100).toFixed(2) } %</td>
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
                                    <td>{ (this.props.infoData.estimatedAvgNegativeSpreadPercentage * 100).toFixed(2) } %</td>
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
                                    <td>{ this.props.infoData.profitCalculation.fiatSpent } € { this.props.infoData.profitCalculation.allFiatSpent ? <span>(ALL IN!)</span> : [] }</td>
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
                                    <td>{ (this.props.infoData.profitCalculation.profitPercentage * 100).toFixed(2) } %</td>
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
                                    <td>{ this.props.infoData.status.buyer.balance.eur }</td>
                                </tr>
                                <tr>
                                    <td>Buyer balance ETH</td>
                                    <td>{ this.props.infoData.status.buyer.balance.eth }</td>
                                </tr>
                                <tr>
                                    <td>Seller balance EUR</td>
                                    <td>{ this.props.infoData.status.seller.balance.eur }</td>
                                </tr>
                                <tr>
                                    <td>Seller balance EUR</td>
                                    <td>{ this.props.infoData.status.seller.balance.eth }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className='row'>
                    <div className='col-sm-12'>
                        <p className='title'>Execute result</p>

                        { this.props.executeData ? <span>{ JSON.stringify(this.props.executeData, null, 2) }</span> : [] }
                    </div>
                </div>
            </div>

        </div>;
    }
}

export default connect(
    (state: ApplicationState) => state.arbitrage,   // Selects which state properties are merged into the component's props
    ArbitrageState.actionCreators                   // Selects which action creators are merged into the component's props
)(Arbitrage) as typeof Arbitrage;
