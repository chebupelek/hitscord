import { rolesApi } from "../Api/rolesApi";
import { notification } from "antd";

const SET_ROLES = "SET_ROLES";
const SET_LOADING_ROLES = "SET_LOADING_ROLES";
const SET_LOADING_CREATE = "SET_LOADING_CREATE";
const SET_LOADING_UPDATE = "SET_LOADING_UPDATE";
const SET_LOADING_DELETE = "SET_LOADING_DELETE";

let initialRolesState = {
    roles: [/*{
        {
            id: "",
            name: "",
            type: 0,
            childRoles: []
        }
    }*/]
  }

const rolesReducer = (state = initialRolesState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_ROLES:
            newState.roles = action.Roles;
            return newState;
        case SET_LOADING_ROLES:
            return {...state, loadingRoles: action.loading};
        case SET_LOADING_CREATE:
            return {...state, loadingCreate: action.loading};
        case SET_LOADING_UPDATE:
            return {...state, loadingUpdate: action.loading};
        case SET_LOADING_DELETE:
            return {...state, loadingDelete: action.loading};
        default:
            return newState;
    }
}

export function getRolesFullActionCreator(data)
{
    return {type: SET_ROLES, Roles: data.roles}
}

export function setLoadingRoles(loading) 
{
    return {type: SET_LOADING_ROLES, loading};
}

export function setLoadingCreate(loading) 
{
    return {type: SET_LOADING_CREATE, loading};
}

export function setLoadingUpdate(loading) 
{
    return {type: SET_LOADING_UPDATE, loading};
}

export function setLoadingDelete(loading) 
{
    return {type: SET_LOADING_DELETE, loading};
}

export function getRolesFullThunkCreator(navigate) 
{
    return async (dispatch) => {
        dispatch(setLoadingRoles(true));
        try 
        {
            const data = await rolesApi.getRoles(navigate);
            if (data) 
            {
                dispatch(getRolesFullActionCreator(data));
            }
        } 
        finally 
        {
            dispatch(setLoadingRoles(false));
        }
    }
}

export function createRoleThunkCreator(data, navigate) 
{
    return async (dispatch) => {
        dispatch(setLoadingCreate(true));
        try 
        {
            const response = await rolesApi.createRole(data, navigate);
            if (response) 
            {
                notification.success({
                    message: "Успех",
                    description: "Роль успешно создана",
                    duration: 4,
                    placement: "topLeft"
                });
                dispatch(getRolesFullThunkCreator(navigate));
            }
        } 
        finally 
        {
            dispatch(setLoadingCreate(false));
        }
    };
}

export function updateRoleThunkCreator(data, navigate) 
{
    return async (dispatch) => {
        dispatch(setLoadingUpdate(true));
        try 
        {
            const response = await rolesApi.updateRole(data, navigate);
            if (response) 
            {
                notification.success({
                    message: "Успех",
                    description: "Роль успешно изменена",
                    duration: 4,
                    placement: "topLeft"
                });
                dispatch(getRolesFullThunkCreator(navigate));
            }
        } 
        finally 
        {
            dispatch(setLoadingUpdate(false));
        }
    };
}

export function deleteRoleThunkCreator(data, navigate) 
{
    return async (dispatch) => {
        dispatch(setLoadingDelete(true));
        try 
        {
            const response = await rolesApi.deleteRole(data, navigate);
            if (response) 
            {
                notification.success({
                    message: "Успех",
                    description: "Роль успешно удалена",
                    duration: 4,
                    placement: "topLeft"
                });
                dispatch(getRolesFullThunkCreator(navigate));
            }
        } 
        finally 
        {
            dispatch(setLoadingDelete(false));
        }
    };
}


export default rolesReducer;