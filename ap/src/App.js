import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';

import Header from "./Models/header/header";
import Base from './Models/Base/Base';
import store from "./Store/store";

function App() {
  return (
    <BrowserRouter basename=''>
      <Provider store={store}>
        <Header/>
        <Base/>
      </Provider>
    </BrowserRouter>
  );
}

export default App;
