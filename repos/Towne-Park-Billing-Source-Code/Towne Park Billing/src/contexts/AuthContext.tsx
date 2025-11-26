import React, { createContext, ReactNode, useContext, useEffect, useRef, useState } from 'react';

interface AuthContextType {
    userRoles: string[];
    userName: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    error: string | null;
    refreshUserData: () => Promise<void>;
    logout: () => void;
}

const defaultAuthContext: AuthContextType = {
    userRoles: [],
    userName: null,
    isAuthenticated: false,
    isLoading: true,
    error: null,
    refreshUserData: async () => { },
    logout: () => { },
};

const AuthContext = createContext<AuthContextType>(defaultAuthContext);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [userRoles, setUserRoles] = useState<string[]>([]);
    const [userName, setUserName] = useState<string | null>(null);
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const fetchInProgress = useRef<Promise<void> | null>(null);

    const fetchUserData = async () => {
        if (fetchInProgress.current) {
            return fetchInProgress.current;
        }

        setIsLoading(true);
        setError(null);

        const fetchPromise = new Promise<void>(async (resolve) => {
            try {
                const response = await fetch('/api/userRole');
                if (!response.ok) {
                    throw new Error('Failed to fetch user data');
                }

                const userData = await response.json();

                setUserRoles(userData.roles || []);

                if (userData.firstName && userData.lastName) {
                    setUserName(`${userData.firstName} ${userData.lastName}`);
                } else if (userData.email) {
                    setUserName(userData.email);
                } else {
                    setUserName(null);
                }

                setIsAuthenticated(true);
            } catch (err) {
                console.error('Error fetching user data:', err);
                setError('Failed to authenticate user');
                setIsAuthenticated(false);
            } finally {
                setIsLoading(false);
                fetchInProgress.current = null;
                resolve();
            }
        });

        fetchInProgress.current = fetchPromise;
        return fetchPromise;
    };

    const refreshUserData = async () => {
        await fetchUserData();
    };

    const logout = () => {
        window.location.href = "/.auth/logout";
    };

    useEffect(() => {
        fetchUserData();
    }, []);

    const value = {
        userRoles,
        userName,
        isAuthenticated,
        isLoading,
        error,
        refreshUserData,
        logout,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => useContext(AuthContext);

export const useUserRoles = () => {
    const { userRoles, isLoading } = useContext(AuthContext);
    return { userRoles, isLoading };
};

export const useUserName = () => {
    const { userName, isLoading } = useContext(AuthContext);
    return { userName, isLoading };
};

export const useIsAuthenticated = () => {
    const { isAuthenticated, isLoading } = useContext(AuthContext);
    return { isAuthenticated, isLoading };
};

export default AuthContext;
