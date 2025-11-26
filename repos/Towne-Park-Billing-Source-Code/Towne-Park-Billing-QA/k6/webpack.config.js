const path = require('path');

module.exports = {
  mode: 'production',
  entry: {
    stress: './src/tests/stress/all-forecast-scenarios.test.ts'
  },
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].bundle.js',
    libraryTarget: 'commonjs',
    clean: true
  },
  resolve: {
    extensions: ['.ts', '.js'],
    alias: {
      '@': path.resolve(__dirname, 'src'),
      '@/types': path.resolve(__dirname, 'src/types'),
      '@/config': path.resolve(__dirname, 'src/config'),
      '@/core': path.resolve(__dirname, 'src/core'),
      '@/utils': path.resolve(__dirname, 'src/utils'),
      '@/scenarios': path.resolve(__dirname, 'src/scenarios'),
      '@/test': path.resolve(__dirname, 'src/test'),
      '@/data': path.resolve(__dirname, 'src/data'),
      '@/stress/steps': path.resolve(__dirname, 'src/scenarios/stress/steps'),
    }
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: {
          loader: 'ts-loader',
          options: {
            configFile: path.resolve(__dirname, 'tsconfig.json')
          }
        },
        exclude: /node_modules/
      },
      {
        test: /\.json$/,
        type: 'json'
      }
    ]
  },
  target: 'web',
  externals: [/^(k6|https:\/\/jslib\.k6\.io\/)/, /^https:\/\/jslib\.k6\.io\/.*$/,
    /^https:\/\/raw\.githubusercontent\.com\/.*$/],
  stats: {
    colors: true,
    chunks: false,
    modules: false
  },
  optimization: {
    minimize: false
  },
  devtool: 'source-map'
};