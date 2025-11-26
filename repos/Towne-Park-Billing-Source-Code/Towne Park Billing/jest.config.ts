module.exports = {
    setupFilesAfterEnv: ["<rootDir>/jest.setup.ts"],
    testEnvironment: "jsdom",
    moduleFileExtensions: ["js", "jsx", "ts", "tsx"],
    moduleNameMapper: {
        "\\.(css|less|scss|sass)$": "identity-obj-proxy",
        "^@/(.*)$": "<rootDir>/src/$1",
        "^src/(.*)$": "<rootDir>/src/$1",
        "\\.svg$": "<rootDir>/svgTransform.tsx"
    },
    transform: {
        "^.+\\.jsx?$": "babel-jest",
        "^.+\\.tsx?$": "ts-jest"
    },
    transformIgnorePatterns: [
        "/node_modules/.+\\.js$"
    ],
    moduleDirectories: ["node_modules", "<rootDir>/src"]
};