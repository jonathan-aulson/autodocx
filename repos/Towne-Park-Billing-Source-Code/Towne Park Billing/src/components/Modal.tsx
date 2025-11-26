import React, { useEffect } from 'react';

interface ModalProps {
    show: boolean;
    onClose: () => void;
    children: React.ReactNode;
    disableBackdropClick?: boolean; // New prop to control backdrop click behavior
}

const Modal: React.FC<ModalProps> = ({ show, onClose, children, disableBackdropClick = false }) => {
    useEffect(() => {
        if (show) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = 'unset';
        }

        return () => {
            document.body.style.overflow = 'unset';
        };
    }, [show]);

    if (!show) return null;

    const handleBackgroundClick = (e: React.MouseEvent) => {
        if (e.target === e.currentTarget && !disableBackdropClick) {
            onClose();
        }
    };

    return (
        <div
        className="fixed inset-0 bg-black bg-opacity-70 flex justify-center items-center z-50"
        onClick={handleBackgroundClick}
        data-qa-id="modal-overlay"
    >
        <div className="bg-white dark:bg-gray-900 max-h-full overflow-auto p-6 rounded-lg">
            <div className="flex justify-end mb-4">
                <button 
                    onClick={onClose} 
                    className="text-black dark:text-white"
                    data-qa-id="button-closeModal"
                >X</button>
            </div>
            {children}
        </div>
    </div>
    );
};

export default Modal;