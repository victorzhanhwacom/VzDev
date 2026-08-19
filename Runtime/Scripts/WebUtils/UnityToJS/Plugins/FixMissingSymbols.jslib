// This file provides the missing symbol required by the WebGL linker.
mergeInto(LibraryManager.library, {
    DownloadFileFromBytes: function (arrayPtr, arrayLength, fileNamePtr) {
        var fileName = UTF8ToString(fileNamePtr);
        var bytes = new Uint8Array(arrayLength);
        bytes.set(new Uint8Array(Module.HEAPU8.buffer, arrayPtr, arrayLength));

        var blob = new Blob([bytes], { type: "application/octet-stream" });
        var link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(link.href);
    }
});