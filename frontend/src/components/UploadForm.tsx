import { ChangeEvent, FormEvent, useRef, useState } from 'react';

interface UploadFormProps {
  onUpload: (file: File) => Promise<void>;
  uploading: boolean;
}

const UploadForm = ({ onUpload, uploading }: UploadFormProps) => {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedName, setSelectedName] = useState<string>('');

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    setSelectedName(file?.name ?? '');
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const file = fileInputRef.current?.files?.[0];
    if (!file) {
      return;
    }
    await onUpload(file);
    setSelectedName('');
    event.currentTarget.reset();
  };

  return (
    <form className="upload" onSubmit={handleSubmit}>
      <h2>Upload a new receipt</h2>
      <p className="upload__hint">Try uploading one of the sample images from the OCR service or your own PNG/JPG.</p>
      <label className="upload__input">
        <input ref={fileInputRef} type="file" accept="image/png,image/jpeg" onChange={handleFileChange} required />
        <span>{selectedName || 'Choose a file…'}</span>
      </label>
      <div className="upload__actions">
        <button type="submit" className="button button--primary" disabled={uploading}>
          {uploading ? 'Uploading…' : 'Upload & Extract'}
        </button>
      </div>
    </form>
  );
};

export default UploadForm;
