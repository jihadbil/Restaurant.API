import type { OrderDto } from '../types/api';

/**
 * Web Printing Service
 * Handles browser print execution for customer receipts and kitchen tickets
 */
export const printService = {
  /**
   * Prints the customer receipt using the browser's thermal print styles
   */
  printReceipt: (_order: OrderDto) => {
    // Add printable class to body and trigger print
    window.print();
  },

  /**
   * Dispatches order printing (Receipt + Kitchen Tickets)
   */
  printOrder: async (_order: OrderDto) => {
    try {
      window.print();
    } catch (ex) {
      console.error('Print error:', ex);
    }
  },
};

export default printService;
