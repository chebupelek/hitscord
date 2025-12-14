const base = import.meta.env.VITE_API_BASE_URL;
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
    icon: `${base}/icon`
};
export default routers;