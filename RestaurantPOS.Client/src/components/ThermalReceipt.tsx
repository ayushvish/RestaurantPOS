import React from 'react';

export interface ThermalReceiptProps {
  order: {
    invoiceNo: string;
    date: string;
    tableNumber: string;
    waiterName: string;
    customerName?: string;
    customerPhone?: string;
    items: Array<{ name: string; qty: number; price: number; amount: number; variant?: string }>;
    subTotal: number;
    cgst: number;
    sgst: number;
    discount: number;
    finalTotal: number;
    paymentMode?: string;
    vpaUri?: string;
    restaurantName?: string;
    gstNumber?: string;
    fssaiNumber?: string;
  };
}

export const ThermalReceipt: React.FC<ThermalReceiptProps> = ({ order }) => {
  const restaurant = order.restaurantName || 'PETPOOJA RESTAURANT POS';
  const upiString = order.vpaUri || `upi://pay?pa=restaurantpos@upi&pn=${encodeURIComponent(restaurant)}&am=${order.finalTotal.toFixed(2)}&tn=Bill_${order.invoiceNo}&cu=INR`;

  return (
    <div id="printable-receipt" className="hidden print:block font-mono text-[11px] leading-tight text-black w-[72mm] mx-auto p-2 bg-white">
      {/* Header */}
      <div className="text-center font-bold text-[14px] uppercase tracking-wide">{restaurant}</div>
      <div className="text-center text-[10px] text-gray-700">
        {order.gstNumber ? `GSTIN: ${order.gstNumber}` : 'GSTIN: 27AABCT3518Q1Z8'}
      </div>
      {order.fssaiNumber && (
        <div className="text-center text-[9px] text-gray-600">FSSAI Lic: {order.fssaiNumber}</div>
      )}
      
      <div className="border-b border-black border-dashed my-2" />

      {/* Meta Info */}
      <div className="flex justify-between text-[10px]">
        <span><strong>Bill:</strong> {order.invoiceNo}</span>
        <span><strong>Table:</strong> {order.tableNumber}</span>
      </div>
      <div className="flex justify-between text-[10px]">
        <span><strong>Date:</strong> {order.date}</span>
        <span><strong>Captain:</strong> {order.waiterName || 'Cashier'}</span>
      </div>
      {order.customerName && (
        <div className="text-[10px] text-left">
          <span><strong>Customer:</strong> {order.customerName} {order.customerPhone ? `(${order.customerPhone})` : ''}</span>
        </div>
      )}

      <div className="border-b border-black border-dashed my-2" />

      {/* Itemized Table */}
      <table className="w-full text-left text-[10px]">
        <thead>
          <tr className="border-b border-black border-dashed">
            <th className="py-1">Item</th>
            <th className="text-center">Qty</th>
            <th className="text-right">Price</th>
            <th className="text-right">Amt</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((item, idx) => (
            <tr key={idx} className="align-top">
              <td className="py-0.5 max-w-[130px] break-words">
                {item.name}
                {item.variant && <div className="text-[8px] text-gray-600 font-sans">({item.variant})</div>}
              </td>
              <td className="text-center py-0.5">{item.qty}</td>
              <td className="text-right py-0.5">{item.price.toFixed(2)}</td>
              <td className="text-right py-0.5">{item.amount.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="border-b border-black border-dashed my-2" />

      {/* Financials */}
      <div className="space-y-0.5 text-[10px]">
        <div className="flex justify-between">
          <span>Subtotal:</span>
          <span>₹{order.subTotal.toFixed(2)}</span>
        </div>
        {order.discount > 0 && (
          <div className="flex justify-between font-bold">
            <span>Discount:</span>
            <span>-₹{order.discount.toFixed(2)}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span>CGST (2.5%):</span>
          <span>₹{order.cgst.toFixed(2)}</span>
        </div>
        <div className="flex justify-between">
          <span>SGST (2.5%):</span>
          <span>₹{order.sgst.toFixed(2)}</span>
        </div>
      </div>

      <div className="border-b-2 border-black my-1.5" />

      <div className="flex justify-between font-black text-[13px]">
        <span>FINAL TOTAL:</span>
        <span>₹{order.finalTotal.toFixed(2)}</span>
      </div>

      {order.paymentMode && (
        <div className="text-[9px] text-left mt-1 text-gray-700">
          Payment Mode: <strong>{order.paymentMode}</strong>
        </div>
      )}

      {/* Bharat UPI QR Code */}
      <div className="mt-3 flex flex-col items-center">
        <img
          src={`https://api.qrserver.com/v1/create-qr-code/?size=110x110&data=${encodeURIComponent(upiString)}`}
          alt="UPI QR Payment"
          className="w-24 h-24 border border-black p-0.5"
        />
        <div className="text-[9px] mt-1 font-semibold">Scan with GPay / PhonePe / Paytm</div>
        <div className="text-[9px] mt-2 font-bold uppercase tracking-wider">*** Thank You! Visit Again ***</div>
      </div>
    </div>
  );
};
