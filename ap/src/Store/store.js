import {legacy_createStore as createStore, combineReducers, applyMiddleware} from 'redux';
import { thunk } from 'redux-thunk';

import headerReducer from '../Reducers/HeaderReducer';
import usersListReducer from '../Reducers/UsersListReducer';
import channelsListReducer from '../Reducers/ChannelsListReducer';
import rolesReducer from '../Reducers/RolesReducer';
import operationsReducer from '../Reducers/OperationsReducer';

let reducers = combineReducers({
    header: headerReducer,
    users: usersListReducer,
    channels: channelsListReducer,
    roles: rolesReducer,
    operations: operationsReducer
});

let store = createStore(reducers, applyMiddleware(thunk));

export default store;