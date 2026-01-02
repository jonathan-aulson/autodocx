<?xml version="1.0" encoding="ASCII"?>
<emulation:EmulationData xmlns:emulation="http:///emulation.ecore" isBW="true" location="MovieSearch.module">
  <ProcessNode Id="moviesearch.module.Process" Name="moviesearch.module.Process" ModelType="BW" moduleName="MovieSearch.module">
    <Operation Name="get" serviceName="movies">
      <Inputs Id="637d5a4a-b395-4b5b-8355-92ad2ab16d5aMovieSearch.module_moviesearch.module.Process_get_getInput" Name="getInput" isDefault="true"/>
    </Operation>
  </ProcessNode>
  <ProcessNode Id="moviesearch.module.SearchOmdb" Name="moviesearch.module.SearchOmdb" ModelType="BW" moduleName="MovieSearch.module">
    <Operation Name="callProcess" serviceName="moviesearch.module.SearchOmdb">
      <Inputs Id="b6e39586-217c-4ad1-8a11-f6ad7b73b5e6MovieSearch.module_moviesearch.module.SearchOmdb_callProcess_Start" Name="Start" isDefault="true" type="CALLPROCESS"/>
    </Operation>
  </ProcessNode>
</emulation:EmulationData>
