---------------------------------------------------------------------
--  Profile Designer DB - Update
--	Date: 2023-04-17
--	Who: SeanC
--	Details:
--	Add support for large file imports
---------------------------------------------------------------------

---------------------------------------------------------------------
--	Rename Table, FKs of child tables, fk col names - import_log becomes import_action
---------------------------------------------------------------------
ALTER TABLE public.import_log
DROP COLUMN file_list;

ALTER TABLE public.import_log
ADD notify_on_complete boolean NOT NULL default('FALSE');

--TODO - --	Rename Table, FKs of child tables, fk col names - import_log becomes import_action

---------------------------------------------------------------------
--	New Table - import file - child table to import_action (formerly import_log)
---------------------------------------------------------------------
-- DROP TABLE public.import_file;
CREATE TABLE public.import_file
(
    id SERIAL PRIMARY KEY,
    import_id integer NOT NULL,  
    file_name character varying NOT NULL,
    total_chunks integer NOT NULL,
    total_bytes bigint NOT NULL,
    CONSTRAINT import_import_id_fk FOREIGN KEY (import_id)
        REFERENCES public.import_log (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        DEFERRABLE INITIALLY DEFERRED
)

TABLESPACE pg_default;

ALTER TABLE public.import_file
    OWNER to profiledesigner;

---------------------------------------------------------------------
--	New Table - import file chunk - child table to import_file
---------------------------------------------------------------------
-- DROP TABLE public.import_file_chunk;
CREATE TABLE public.import_file_chunk
(
    id SERIAL PRIMARY KEY,
    import_file_id integer NOT NULL,  
    chunk_order integer NOT NULL,
    contents text NULL,
    CONSTRAINT import_file_import_file_id_fk FOREIGN KEY (import_file_id)
        REFERENCES public.import_file (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        DEFERRABLE INITIALLY DEFERRED
)

TABLESPACE pg_default;

ALTER TABLE public.import_file_chunk
    OWNER to profiledesigner;

