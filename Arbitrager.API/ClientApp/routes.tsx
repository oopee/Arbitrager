import * as React from 'react';
import { Route } from 'react-router-dom';
import { MainLayout } from './components/MainLayout';
import Home from './components/Home';
import Arbitrage from './components/Arbitrage';

export const routes = <MainLayout>
    <Route exact path='/' component={ Arbitrage } />
</MainLayout>;
