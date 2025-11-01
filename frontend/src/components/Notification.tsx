interface NotificationProps {
  type: 'success' | 'error' | 'info';
  message: string;
  onClose?: () => void;
}

const Notification = ({ type, message, onClose }: NotificationProps) => {
  return (
    <div className={`toast toast--${type}`} role="status">
      <span>{message}</span>
      {onClose ? (
        <button type="button" className="toast__close" onClick={onClose}>
          ×
        </button>
      ) : null}
    </div>
  );
};

export default Notification;
