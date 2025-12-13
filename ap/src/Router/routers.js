const base = "https://166664.msk.web.highserver.ru/api/admin";
const routers = {
    login: `${base}/login`,
    logout: `${base}/logout`,
    users: `${base}/users/list`,
    rolesShort: `${base}/roles/list/short`,
    addRole: `${base}/roles/add`,
    removeRole: `${base}/roles/remove`,
    getChannels: `${base}/deletedChannels/list`,
    rewiveChannel: `${base}/deletedChannels/rewive`,
    rolesFull: `${base}/roles/list/full`,
    roleCreate: `${base}/roles/create`,
    roleUpdate: `${base}/roles/rename`,
    roleDelete: `${base}/roles/delete`,
};
export default routers;