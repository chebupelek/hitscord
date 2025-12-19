import { operationsApi } from "../Api/operationsApi";

const SET_OPEARTIONS = "SET_OPEARTIONS";
const SET_LOADING_OPERATIOS = "SET_LOADING_OPERATIOS";

let initialOperationsState = {
    operations: [/*{
        {
            id: "",
            opaerationTime: Date(),
            adminName: "",
            operation: "",
            operationData: ""
        }
    }*/],
    pagination: {
        Page: 0,
        Number: 0,
        PageCount: 0,
        NumberCount: 0
    },
    loadingOperations: false
  }

const operationsReducer = (state = initialOperationsState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_OPEARTIONS:
            newState.operations = action.Operations;
            newState.pagination.Page = action.Page;
            newState.pagination.Number = action.Number;
            newState.pagination.PageCount = action.PageCount;
            newState.pagination.NumberCount = action.NumberCount;
            return newState;
        case SET_LOADING_OPERATIOS:
            newState.loadingOperations = action.value;
            return newState;
        default:
            return newState;
    }
}

export function setLoadingOperationsActionCreator(value) {return { type: SET_LOADING_OPERATIOS, value }}

export function getOperationsActionCreator(data){
    return {type: SET_OPEARTIONS, Operations: data.operations, Page: data.page, Number: data.number, PageCount: data.pageCount, NumberCount: data.numberCount}
}

export function getOperationsThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingOperationsActionCreator(true));
        return operationsApi.getOperations(queryParams, navigate)
            .then(data => {
                if (!data)
                {
                   return; 
                }
                dispatch(getOperationsActionCreator(data));
            })
            .finally(() => dispatch(setLoadingOperationsActionCreator(false)));
    }
}

export default operationsReducer;