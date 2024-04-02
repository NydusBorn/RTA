export default defineEventHandler((event) => {
    if (process.env.BACKEND_HOST == undefined) {
        return {host: "http://localhost:5043"}
    }
    else{
        return {host: process.env.BACKEND_HOST}
    }
})