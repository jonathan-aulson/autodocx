module.exports = {
  presets: [
    '@babel/preset-env',  // Compile modern JavaScript
    '@babel/preset-react' // Compile JSX to JavaScript
  ],
  plugins: [
    '@babel/plugin-proposal-class-properties',
    '@babel/plugin-proposal-private-methods',
    '@babel/plugin-proposal-private-property-in-object'
  ]
};
