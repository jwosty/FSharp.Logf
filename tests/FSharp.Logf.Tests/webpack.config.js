var path = require("path");

function resolve(relativePath) {
    return path.join(__dirname, relativePath);
}

module.exports = {
    entry: resolve('./Program.fs.js'),
    target: 'node',
    mode: 'production',
    output: {
        path: __dirname,
        filename: 'bin/mocha/bundle.js',
    },
    module: {
        rules: [
            // - mocha-loader: 
            {
                test: /test\.js$/,
                use: 'mocha-loader',
                exclude: /node_modules/,
            },
        ],
    },
};