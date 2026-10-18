import React, { useState, useEffect } from 'react';
import Modal from '../../../components/ui/Modal';
import Button from '../../../components/ui/Button';
import Input from '../../../components/ui/Input';
import type { AddonDto } from '../../../types/api';

interface ExcludeAddonsModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (notes: string) => void;
  productName: string;
  currentNotes: string;
  availableAddons: AddonDto[];
}

export const ExcludeAddonsModal: React.FC<ExcludeAddonsModalProps> = ({
  isOpen,
  onClose,
  onSave,
  productName,
  currentNotes,
  availableAddons,
}) => {
  const [selectedExclusions, setSelectedExclusions] = useState<string[]>([]);
  const [customNote, setCustomNote] = useState<string>('');

  useEffect(() => {
    if (isOpen) {
      const timer = window.setTimeout(() => {
      if (currentNotes && currentNotes !== 'لا يوجد ملاحظات') {
        setCustomNote(currentNotes);
      } else {
        setCustomNote('');
      }
      setSelectedExclusions([]);
      }, 0);
      return () => window.clearTimeout(timer);
    }
  }, [isOpen, currentNotes]);

  const toggleExclusion = (addonName: string) => {
    setSelectedExclusions((prev) =>
      prev.includes(addonName) ? prev.filter((name) => name !== addonName) : [...prev, addonName]
    );
  };

  const handleApply = () => {
    const parts: string[] = [];

    if (selectedExclusions.length > 0) {
      parts.push(`بدون: ${selectedExclusions.join('، ')}`);
    }

    if (customNote.trim()) {
      parts.push(customNote.trim());
    }

    const finalNotes = parts.length > 0 ? parts.join(' - ') : 'لا يوجد ملاحظات';
    onSave(finalNotes);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={`تخصيص الصنف: ${productName}`}
      maxWidth="500px"
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
        {/* Quick Exclusions from Addons */}
        {availableAddons.length > 0 && (
          <div>
            <label style={{ fontSize: '0.875rem', fontWeight: 700, display: 'block', marginBottom: '8px' }}>
              استبعاد مكونات أو إضافات سريعة:
            </label>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
              {availableAddons.map((addon) => {
                const isSelected = selectedExclusions.includes(addon.name);
                return (
                  <button
                    key={addon.id}
                    type="button"
                    onClick={() => toggleExclusion(addon.name)}
                    style={{
                      padding: '8px 14px',
                      borderRadius: 'var(--radius-full)',
                      border: `1.5px solid ${isSelected ? 'var(--color-danger)' : 'var(--color-border)'}`,
                      backgroundColor: isSelected ? 'var(--color-danger-bg)' : 'var(--color-surface)',
                      color: isSelected ? 'var(--color-danger-text)' : 'var(--color-text-primary)',
                      fontSize: '0.875rem',
                      fontWeight: 600,
                      cursor: 'pointer',
                      transition: 'all var(--transition-fast)',
                    }}
                  >
                    {isSelected ? `✖ بدون ${addon.name}` : `بدون ${addon.name}`}
                  </button>
                );
              })}
            </div>
          </div>
        )}

        {/* Custom Text Notes */}
        <div>
          <Input
            label="ملاحظات تحضير خاصة"
            placeholder="مثال: تسوية زيادة، كاتشب خارجي، شطة خفيفة..."
            value={customNote}
            onChange={(e) => setCustomNote(e.target.value)}
          />
        </div>

        {/* Action Buttons */}
        <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end', marginTop: '10px' }}>
          <Button variant="secondary" onClick={onClose}>
            إلغاء
          </Button>
          <Button variant="primary" onClick={handleApply}>
            تطبيق الملاحظة
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ExcludeAddonsModal;
