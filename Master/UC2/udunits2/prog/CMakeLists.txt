add_executable (udunits2p udunits2.c)
target_link_libraries (udunits2p udunits2)
install (TARGETS udunits2p
	 RUNTIME DESTINATION bin)
