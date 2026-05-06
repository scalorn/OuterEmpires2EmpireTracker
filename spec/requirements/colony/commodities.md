# Commodity Requests

**REQ-COL-070** The user SHALL be able to add a commodity request by selecting a commodity from a filtered combo and clicking Add.  
**REQ-COL-071** A new CommodityRequested SHALL be created with the commodity Name, Requested quantity from the quantity field, NeedBy date from a date field, Delivered=0, and Fulfilled=false. The quantity SHALL be passed through the ViewModel — the form SHALL NOT set it directly on the returned object.  
**REQ-COL-071b** Existing commodity requests SHALL be editable in-place via the grid. Editing the Requested amount cell SHALL immediately update the backing CommodityRequested object through CellValueChanged.  
**REQ-COL-072** The commodity requests grid SHALL display Name, Requested amount, Delivered amount, and NeedBy date for each request.  
**REQ-COL-073** Editing the Amount cell in the grid SHALL immediately update the Requested field of the backing CommodityRequested object.  
**REQ-COL-074** Pressing the Delete key when a commodity request row is selected SHALL remove that request from the colony.  
**REQ-COL-075** The commodity requests grid SHALL use FullRowSelect mode so that clicking any cell selects the entire row.
