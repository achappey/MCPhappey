import "react-json-view-lite/dist/index.css";
type ResourceModalProps = {
    show: boolean;
    onClose: () => void;
    resourceName: string;
    mimeType: string | undefined;
    content: any;
    loading: boolean;
    error: string | null;
};
declare const ResourceModal: ({ show, onClose, resourceName, mimeType, content, loading, error, }: ResourceModalProps) => import("react/jsx-runtime").JSX.Element;
export default ResourceModal;
//# sourceMappingURL=ResourceModal.d.ts.map