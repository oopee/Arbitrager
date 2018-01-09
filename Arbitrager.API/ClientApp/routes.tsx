import * as React from 'react';
import { Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import Home from './components/Home';
import Arbitrage from './components/Arbitrage';

export const routes = <Layout>
    <Route exact path='/' component={ Arbitrage } />
</Layout>;
