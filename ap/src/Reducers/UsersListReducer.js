import { usersApi } from "../Api/usersApi";
import { notification } from "antd";

const SET_USERS = "SET_USERS";
const SET_ROLES = "SET_ROLES";
const SET_LOADING_USERS = "SET_LOADING_USERS";
const SET_LOADING_ROLES_SHORT = "SET_LOADING_ROLES_SHORT";
const SET_LOADING_ADD_ROLE = "SET_LOADING_ADD_ROLE";
const SET_LOADING_REMOVE_ROLE = "SET_LOADING_REMOVE_ROLE";

let initialUsersListState = {
    users: [/*{
        {
            id: "",
            mail: "",
            accountName: "",
            accountTag: "",
            accountCreateDate: Date(),
            notifiable: false,
            friendshipApplication: false,
            nonFriendMessage: false,
            icon: {
                fileId: "",
                fileName: "",
                fileType: "",
                fileSize: 0,
                deleted: false
            },
            systemRoles: [
                {
                    id: "",
                    name: "",
                    type: 0
                }
            ]
        }
    }*/],
    pagination: {
        Page: 0,
        Number: 0,
        PageCount: 0,
        NumberCount: 0
    },
    rolesShort: [/*{
        {
            id: "",
            name: "",
            type: 0,
            childRoles: []
        }
    }*/],
    loadingUsers: false,
    loadingRolesShort: false,
    loadingAddRole: false,
    loadingRemoveRole: false
  }

const usersListReducer = (state = initialUsersListState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_USERS:
            newState.users = action.Users;
            newState.pagination.Page = action.Page;
            newState.pagination.Number = action.Number;
            newState.pagination.PageCount = action.PageCount;
            newState.pagination.NumberCount = action.NumberCount;
            return newState;
        case SET_ROLES:
            newState.roles = action.Roles;
            return newState;
        case SET_LOADING_USERS:
            newState.loadingUsers = action.value;
            return newState;
        case SET_LOADING_ROLES_SHORT:
            newState.loadingRolesShort = action.value;
            return newState;
        case SET_LOADING_ADD_ROLE:
            newState.loadingAddRole = action.value;
            return newState;
        case SET_LOADING_REMOVE_ROLE:
            newState.loadingRemoveRole = action.value;
            return newState;
        default:
            return newState;
    }
}

export function setLoadingUsersActionCreator(value) {return { type: SET_LOADING_USERS, value }}
export function setLoadingRolesShortActionCreator(value) {return { type: SET_LOADING_ROLES_SHORT, value }}
export function setLoadingAddRoleActionCreator(value) {return { type: SET_LOADING_ADD_ROLE, value }}
export function setLoadingRemoveRoleActionCreator(value) {return { type: SET_LOADING_REMOVE_ROLE, value }}

export function getUsersListActionCreator(data){
    return {type: SET_USERS, Users: data.users, Page: data.page, Number: data.number, PageCount: data.pageCount, NumberCount: data.numberCount}
}

export function getUsersListThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingUsersActionCreator(true));
        return usersApi.getUsers(queryParams, navigate)
            .then(data => {
                if (!data)
                {
                   return; 
                }
                dispatch(getUsersListActionCreator(data));
            })
            .finally(() => dispatch(setLoadingUsersActionCreator(false)));
    }
}

export function getRolesShortListActionCreator(data){
    return {type: SET_ROLES, Roles: data.roles}
}

export function getRolesShortListThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingRolesShortActionCreator(true));
        return usersApi.getRolesShort(queryParams, navigate)
            .then(data => {
                if (!data)
                {
                    return;
                }
                dispatch(getRolesShortListActionCreator(data));
            })
            .finally(() => dispatch(setLoadingRolesShortActionCreator(false)));
    }
}

export function addRoleThunkCreator(data, navigate, resetState, closeModal, reloadUsers) {
    return (dispatch) => {
        dispatch(setLoadingAddRoleActionCreator(true));
        return usersApi.addRoles(data, navigate)
            .then(response => {
                if (response !== null) 
                {
                    notification.success({
                        message: "Успех",
                        description: "Роль успешно добавлена",
                        duration: 4,
                        placement: "topLeft"
                    });
                    closeModal();
                    resetState();
                    reloadUsers();
                }
            })
            .finally(() => dispatch(setLoadingAddRoleActionCreator(false)));
    };
}

export function removeRoleThunkCreator(data, navigate, reloadUsers) {
    return (dispatch) => {
        dispatch(setLoadingRemoveRoleActionCreator(true));
        return usersApi.removeRole(data, navigate)
            .then(response => {
                if (response !== null) 
                {
                    notification.success({
                        message: "Успех",
                        description: "Роль успешно изъята",
                        duration: 4,
                        placement: "topLeft"
                    });
                    reloadUsers();
                }
            })
            .finally(() => dispatch(setLoadingRemoveRoleActionCreator(false)));
    };
}

export default usersListReducer;