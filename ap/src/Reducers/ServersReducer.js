import { serverApi } from "../Api/serverApi";
import { iconApi } from "../Api/iconApi";
import { notification } from "antd";

const SET_SERVERS = "SET_SERVERS";
const SET_SERVER_DATA = "SET_SERVER_DATA";
const SET_LOADING_SERVERS = "SET_LOADING_SERVERS";
const SET_LOADING_SERVERDATA = "SET_LOADING_SERVERDATA";

let initialServersListState = {
    servers: [
        /*
        {
            id: "",
            serverName: "",
            serverType: 0, // ServerTypeEnum: 0 - Student, 1 - Teacher
            usersNumber: 0,
            icon: {
                fileId: "",
                fileName: "",
                fileType: "",
                fileSize: 0,
                deleted: false
            }
        }
        */
    ],
    pagination: {
        page: 0,
        number: 0,
        pageCount: 0,
        numberCount: 0
    },
    serverData: null,
    /*
    {
        serverId: "",
        serverName: "",
        serverType: 0, // ServerTypeEnum
        icon: {
            fileId: "",
            fileName: "",
            fileType: "",
            fileSize: 0,
            deleted: false
        },
        isClosed: false,
        roles: [
            {
                id: "",
                name: "",
                tag: "",
                color: "",
                type: 0, // RoleEnum
                permissions: {
                    canChangeRole: false,
                    canWorkChannels: false,
                    canDeleteUsers: false,
                    canMuteOther: false,
                    canDeleteOthersMessages: false,
                    canIgnoreMaxCount: false,
                    canCreateRoles: false,
                    canCreateLessons: false,
                    canCheckAttendance: false
                },
                channelCanSee: [{ id: "", name: "" }],
                channelCanWrite: [{ id: "", name: "" }],
                channelCanWriteSub: [{ id: "", name: "" }],
                channelNotificated: [{ id: "", name: "" }],
                channelCanUse: [{ id: "", name: "" }],
                channelCanJoin: [{ id: "", name: "" }]
            }
        ],
        presets: [
            {
                serverRoleId: "",
                serverRoleName: "",
                systemRoleId: "",
                systemRoleName: "",
                systemRoleType: 0 // SystemRoleTypeEnum
            }
        ],
        users: [
            {
                serverId: "",
                userId: "",
                userName: "",
                userTag: "",
                icon: {
                    fileId: "",
                    fileName: "",
                    fileType: "",
                    fileSize: 0,
                    deleted: false
                },
                isBanned: false,
                banReason: null,
                banTime: null,
                nonNotifiable: false,
                roles: [
                    {
                        roleId: "",
                        roleName: "",
                        roleType: 0, // RoleEnum
                        colour: ""
                    }
                ],
                systemRoles: [
                    {
                        id: null,
                        name: "",
                        type: 0 // SystemRoleTypeEnum
                    }
                ]
            }
        ],
        channels: {
            textChannels: [
                {
                    channelId: "",
                    channelName: "",
                    messagesNumber: 0
                }
            ],
            voiceChannels: [
                {
                    channelId: "",
                    channelName: "",
                    maxCount: 0
                }
            ],
            notificationChannels: [
                {
                    channelId: "",
                    channelName: "",
                    messagesNumber: 0
                }
            ],
            pairVoiceChannels: [
                {
                    channelId: "",
                    channelName: "",
                    maxCount: 0
                }
            ]
        }
    }
    */
    loadingServers: false,
    loadingServerData: false
};

const serversReducer = (state = initialServersListState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_SERVERS:
            newState.servers = action.Servers;
            newState.pagination.Page = action.Page;
            newState.pagination.Number = action.Number;
            newState.pagination.PageCount = action.PageCount;
            newState.pagination.NumberCount = action.NumberCount;
            return newState;
        case SET_SERVER_DATA:
            newState.serverData = action.ServerData;
            return newState;
        case SET_LOADING_SERVERS:
            newState.loadingServers = action.value;
            return newState;
        case SET_LOADING_SERVERDATA:
            newState.loadingServerData = action.value;
            return newState;
        default:
            return newState;
    }
}

export function setLoadingServersActionCreator(value) {return { type: SET_LOADING_SERVERS, value }}
export function setLoadingServerDataShortActionCreator(value) {return { type: SET_LOADING_SERVERDATA, value }}

export function getServersListActionCreator(data){
    return {type: SET_SERVERS, Servers: data.servers, Page: data.page, Number: data.number, PageCount: data.pageCount, NumberCount: data.numberCount}
}

export function getServersListThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingServersActionCreator(true));
        return serverApi.getServersList(queryParams, navigate)
            .then(data => {
                if (!data)
                {
                   return; 
                }
                dispatch(getServersListActionCreator(data));
            })
            .finally(() => dispatch(setLoadingServersActionCreator(false)));
    }
}

export function getServerDataActionCreator(data){
    return {type: SET_SERVER_DATA, ServerData: data}
}

export function clearServerDataActionCreator(){
    return {type: SET_SERVER_DATA, ServerData: null}
}

export function getServerDataThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingServerDataShortActionCreator(true));
        return serverApi.getServerData(queryParams, navigate)
            .then(data => {
                if (!data)
                {
                   return; 
                }
                dispatch(getServerDataActionCreator(data));
            })
            .finally(() => dispatch(setLoadingServerDataShortActionCreator(false)));
    }
}

export default serversReducer;